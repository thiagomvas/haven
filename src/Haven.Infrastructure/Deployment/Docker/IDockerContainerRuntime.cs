using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.Deployment;

/// <summary>
/// Low-level Docker container lifecycle primitives (parameter building, create+start, label
/// lookup, network connect, stop+remove) shared by anything that owns Docker containers.
/// Operates on ids, labels and Docker.DotNet types only — no dependency on the <c>Service</c>
/// aggregate — so it can be reused by future non-Service container owners (e.g. sidecars).
/// </summary>
public interface IDockerContainerRuntime
{
    /// <summary>
    /// Builds the <see cref="CreateContainerParameters"/> for a container: name/labels/image,
    /// environment variables (plus <c>LISTEN_ADDRESS</c> when <paramref name="exposureMode"/>
    /// requires one), port bindings parsed from <paramref name="ports"/>, and volume mounts.
    /// </summary>
    CreateContainerParameters BuildContainerParameters(
        string name,
        IDictionary<string, string> labels,
        string image,
        IEnumerable<EnvironmentVariables>? envs,
        ExposureMode exposureMode,
        IReadOnlyList<string> ports,
        IList<Mount> mounts);

    /// <summary>Creates a container from <paramref name="parameters"/> and starts it. Returns the new container id.</summary>
    Task<Result<string>> CreateAndStartAsync(CreateContainerParameters parameters, CancellationToken cancellationToken);

    /// <summary>Best-effort connects <paramref name="ownerId"/> to <paramref name="networkIds"/> via <paramref name="networkingService"/>. Failures are logged, never thrown.</summary>
    Task ConnectToNetworksAsync(Guid ownerId, IReadOnlyCollection<Guid> networkIds, INetworkingService networkingService, CancellationToken cancellationToken);

    /// <summary>Lists all containers (running or not) carrying the given label.</summary>
    Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label, CancellationToken cancellationToken);

    /// <summary>Disconnects <paramref name="ownerId"/> from all networks, then stops (best-effort, swallowing timeouts) and force-removes each container, logging <paramref name="reason"/> per container.</summary>
    Task StopAndRemoveAsync(IReadOnlyCollection<ContainerListResponse> containers, Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken);

    /// <summary>Finds containers labeled with <paramref name="ownerId"/>'s id label and, if any exist, stops and removes them. No-op when none exist.</summary>
    Task RemoveAllForOwnerAsync(Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken);

    /// <summary>Finds the container labeled with <paramref name="serviceId"/>'s id label and inspects it. Fails with <see cref="Error.Docker"/>.ContainerNotFound when none exists.</summary>
    Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken);
}