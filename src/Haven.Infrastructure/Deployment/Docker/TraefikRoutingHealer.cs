using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>
/// Verifies that Traefik is actually routing a Traefik-registered service to its current, correct
/// container IP - not just that the container is attached to the right Docker network(s) (which
/// <see cref="TraefikLabelMerger"/> and <see cref="NetworkReconciliationService"/> already ensure).
/// Traefik's Docker provider only refreshes its routing table on container lifecycle events (start,
/// die, health_status); it never reacts to network connect/disconnect events on an already-running
/// container. So a container can be attached to exactly the right network, with exactly the right
/// <c>traefik.docker.network</c> label, and Traefik can *still* be serving traffic to a stale IP it
/// cached at some earlier point (e.g. a race during the container's own startup, or any Docker-level
/// network operation performed on a running container rather than through a full recreate). When
/// that happens, network reconciliation alone can never detect or fix it, because from Docker's
/// point of view everything already looks correct - only Traefik's own runtime state is wrong.
/// This heals that class of drift by asking Traefik's API what it's actually resolving and, if it
/// doesn't match reality, forcing a full provider reload via a container restart (not a recreate -
/// same container, same id, just a fresh read of current Docker state).
/// </summary>
public interface ITraefikRoutingHealer
{
    /// <summary>
    /// Checks whether Traefik is currently routing <paramref name="serviceId"/> to
    /// <paramref name="expectedIpAddress"/> and, if not, restarts the Traefik sidecar to force it to
    /// re-resolve. Best-effort throughout: Traefik being unreachable, the service not being
    /// registered with it yet, or the service not being Traefik-routed at all are all treated as
    /// "nothing to heal" rather than errors. Returns <see langword="true"/> only when a mismatch was
    /// found and a restart was actually issued.
    /// </summary>
    Task<bool> VerifyAndHealAsync(Guid serviceId, string? expectedIpAddress, CancellationToken cancellationToken = default);
}

public sealed class TraefikRoutingHealer(
    ISidecarRepository sidecarRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    ITraefikApiClient traefikApiClient,
    IDockerContainerRuntime containerRuntime,
    ILogger<TraefikRoutingHealer> logger) : ITraefikRoutingHealer
{
    // Traefik's Docker provider debounces before applying a config change, so a check made the
    // instant a container starts can catch it mid-transition even when everything is actually fine.
    // Retry a few times before concluding it's genuinely stuck rather than just not-yet-settled.
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
    ];

    public async Task<bool> VerifyAndHealAsync(Guid serviceId, string? expectedIpAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expectedIpAddress))
            return false;

        var sidecars = await sidecarRepository.GetAllAsync(cancellationToken);
        var traefik = sidecars.FirstOrDefault(s => s.Kind == SidecarKind.Traefik);
        if (traefik is not { Enabled: true })
            return false;

        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(serviceId, cancellationToken);
        if (entry is null || entry.Domains.Count == 0)
            return false;

        var disambiguate = entry.Domains.Count > 1;

        for (var attempt = 0; ; attempt++)
        {
            var checkedAny = false;
            var mismatched = false;

            foreach (var domain in entry.Domains)
            {
                var routerName = domain.RouterName(entry.ContainerName, disambiguate);
                var urlsResult = await traefikApiClient.GetServiceServerUrlsAsync(routerName, cancellationToken);
                if (urlsResult.IsFailure || urlsResult.Value.Count == 0)
                    continue;

                checkedAny = true;
                if (!urlsResult.Value.Any(url => url.Contains(expectedIpAddress, StringComparison.OrdinalIgnoreCase)))
                    mismatched = true;
            }

            // Traefik unreachable, or this service isn't registered with it yet - nothing we can
            // confirm is wrong, so don't restart on a guess.
            if (!checkedAny)
                return false;

            if (!mismatched)
                return false;

            if (attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], cancellationToken);
                continue;
            }

            logger.LogWarning(
                "Traefik is still routing service {ServiceId} to a stale backend after {Attempts} verification attempts (expected {ExpectedIp}); restarting Traefik sidecar {SidecarId} to force a provider refresh",
                serviceId, attempt + 1, expectedIpAddress, traefik.Id);

            var restartResult = await containerRuntime.RestartByServiceIdAsync(traefik.Id, cancellationToken);
            if (restartResult.IsFailure)
            {
                logger.LogWarning(
                    "Failed to auto-restart Traefik sidecar {SidecarId} to heal stale routing for service {ServiceId}: {Error}",
                    traefik.Id, serviceId, restartResult.Error.Message);
                return false;
            }

            logger.LogInformation(
                "Restarted Traefik sidecar {SidecarId} to heal stale routing for service {ServiceId}",
                traefik.Id, serviceId);
            return true;
        }
    }
}
