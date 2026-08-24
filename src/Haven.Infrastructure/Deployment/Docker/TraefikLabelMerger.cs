using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Utils;

namespace Haven.Infrastructure.Deployment.Docker;

public interface ITraefikLabelMerger
{
    /// <summary>
    /// Merges <c>traefik.*</c> labels into <paramref name="labels"/> when the Traefik sidecar is
    /// enabled, so the service is only discoverable by Traefik if and only if Traefik itself is on.
    /// Shared by every <see cref="Haven.Application.Common.Interfaces.Deployment.IDeployService"/>
    /// implementation so Traefik wiring can never drift between source config types.
    /// </summary>
    Task MergeAsync(Service service, Dictionary<string, string> labels, CancellationToken cancellationToken = default);
}

public class TraefikLabelMerger(
    ISidecarRepository sidecarRepository,
    IServiceRegistryEntryRepository serviceRegistryEntryRepository,
    INetworkRepository networkRepository) : ITraefikLabelMerger
{
    public async Task MergeAsync(Service service, Dictionary<string, string> labels, CancellationToken cancellationToken = default)
    {
        var sidecars = await sidecarRepository.GetAllAsync(cancellationToken);
        var traefik = sidecars.FirstOrDefault(s => s.Kind == SidecarKind.Traefik);
        if (traefik is not { Enabled: true }) return;

        var entry = await serviceRegistryEntryRepository.GetForServiceAsync(service.Id, cancellationToken);
        var traefikLabels = DockerUtils.BuildTraefikLabels(entry);
        if (traefikLabels.Count == 0) return;

        foreach (var (key, value) in traefikLabels)
            labels[key] = value;

        // The service container also joins the default "bridge" network (Docker's implicit default
        // for any container) alongside its Project/Environment network. Traefik's Docker provider
        // can't reliably tell which of a multi-network container's networks to route through unless
        // told explicitly, so pin it to the Project/Environment network by name.
        var environment = service.Environment;
        if (environment != null)
        {
            var networks = await networkRepository.GetByProjectAndEnvironmentAsync(environment.ProjectId, environment.Id, cancellationToken);
            var network = networks.FirstOrDefault();
            if (network != null)
                labels["traefik.docker.network"] = network.Name;
        }
    }
}
