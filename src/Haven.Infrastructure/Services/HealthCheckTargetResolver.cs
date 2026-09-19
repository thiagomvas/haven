using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Utils;

namespace Haven.Infrastructure.Services;

/// <param name="ContainerName">Docker container name, resolvable by DNS from other containers on the same network.</param>
/// <param name="Port">Lowest container port the container exposes, if any.</param>
/// <param name="NetworkName">User-defined network the container is attached to (preferring Haven's own networks).</param>
/// <param name="IsRunning">Whether the container is currently running.</param>
public sealed record HealthCheckTarget(string ContainerName, int? Port, string? NetworkName, bool IsRunning);

public interface IHealthCheckTargetResolver
{
    Task<Result<HealthCheckTarget>> ResolveAsync(Guid serviceId, CancellationToken cancellationToken);
}

public sealed class HealthCheckTargetResolver(IDockerContainerRuntime containerRuntime) : IHealthCheckTargetResolver
{
    private static readonly HashSet<string> BuiltInNetworks = ["bridge", "host", "none"];

    public async Task<Result<HealthCheckTarget>> ResolveAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var inspected = await containerRuntime.InspectByServiceIdAsync(serviceId, cancellationToken);
        if (inspected.IsFailure)
            return inspected.Error;

        return FromInspect(inspected.Value);
    }

    internal static HealthCheckTarget FromInspect(ContainerInspectResponse container)
    {
        var name = container.Name.TrimStart('/');

        int? port = container.NetworkSettings?.Ports is null
            ? null
            : container.ExtractPortMappings().Select(p => (int?)p.ContainerPort).Min();

        var networks = container.NetworkSettings?.Networks?.Keys
            .Where(n => !BuiltInNetworks.Contains(n))
            .ToList() ?? [];
        var network = networks.FirstOrDefault(n => n.StartsWith("haven-", StringComparison.Ordinal) && n != "haven-system")
                      ?? networks.FirstOrDefault();

        return new HealthCheckTarget(name, port, network, container.State?.Running ?? false);
    }
}
