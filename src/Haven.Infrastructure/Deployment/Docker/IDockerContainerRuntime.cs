using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using RestartPolicy = Haven.Domain.Enums.RestartPolicy;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>
/// Docker container lifecycle primitives (parameter building, create+start, label lookup, network
/// connect, stop+remove) shared by anything that owns Docker containers, plus the higher-level
/// <c>Service</c>-aware helpers (env/mount/label assembly, Project/Environment network resolution,
/// deploy-data extraction, Traefik self-heal) shared by the Service-level Docker deploy services
/// (<see cref="DockerContainerDeployService"/>, <see cref="DockerfileDeployService"/>).
/// </summary>
public interface IDockerContainerRuntime
{
    /// <summary>
    /// Builds the <see cref="CreateContainerParameters"/> for a container: name/labels/image,
    /// environment variables (plus <c>LISTEN_ADDRESS</c> when <paramref name="exposureMode"/>
    /// requires one), port bindings parsed from <paramref name="ports"/>, volume mounts, and an
    /// optional <c>Cmd</c> override built from <paramref name="commandArgs"/> (falls back to the
    /// image's default <c>CMD</c> when empty).
    /// </summary>
    CreateContainerParameters BuildContainerParameters(
        string name,
        IDictionary<string, string> labels,
        string image,
        IEnumerable<EnvironmentVariables>? envs,
        ExposureMode exposureMode,
        IReadOnlyList<string> ports,
        IList<Mount> mounts,
        RestartPolicy restartPolicy,
        IReadOnlyList<string> commandArgs);

    /// <summary>Creates a container from <paramref name="parameters"/> and starts it. Returns the new container id.</summary>
    Task<Result<string>> CreateAndStartAsync(CreateContainerParameters parameters, CancellationToken cancellationToken);

    /// <summary>Best-effort connects <paramref name="ownerId"/> to <paramref name="networkIds"/> via <paramref name="networkingService"/>. Failures are logged, never thrown.</summary>
    Task ConnectToNetworksAsync(Guid ownerId, IReadOnlyCollection<Guid> networkIds, INetworkingService networkingService, CancellationToken cancellationToken);

    /// <summary>Connects an arbitrary (not necessarily Haven-owned) container to a Docker network by raw id, e.g. Haven's own container joining the system network.</summary>
    Task<Result> ConnectContainerToNetworkAsync(string containerId, string dockerNetworkId, CancellationToken cancellationToken);

    /// <summary>Lists all containers (running or not) carrying the given label.</summary>
    Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label, CancellationToken cancellationToken);

    /// <summary>Disconnects <paramref name="ownerId"/> from all networks, then stops (best-effort, swallowing timeouts) and force-removes each container, logging <paramref name="reason"/> per container.</summary>
    Task StopAndRemoveAsync(IReadOnlyCollection<ContainerListResponse> containers, Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken);

    /// <summary>Finds containers labeled with <paramref name="ownerId"/>'s id label and, if any exist, stops and removes them. No-op when none exist.</summary>
    Task RemoveAllForOwnerAsync(Guid ownerId, INetworkingService networkingService, string reason, CancellationToken cancellationToken);

    /// <summary>Finds the container labeled with <paramref name="serviceId"/>'s id label and inspects it. Fails with <see cref="Error.Docker"/>.ContainerNotFound when none exists.</summary>
    Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the container labeled with <paramref name="ownerId"/>'s id label and restarts it in
    /// place (stop + start, same container/id - not a recreate). Used to force a sidecar like Traefik
    /// to fully reload its provider state (e.g. after network topology drift its event-driven Docker
    /// provider never observed) without going through a full redeploy. Fails with
    /// <see cref="Error.Docker"/>.ContainerNotFound when no container exists for the owner.
    /// </summary>
    Task<Result> RestartByServiceIdAsync(Guid ownerId, CancellationToken cancellationToken);

    /// <summary>
    /// Ensures every named-volume mount in <paramref name="mounts"/> exists and, for volumes that
    /// don't exist yet, fixes ownership to match <paramref name="image"/>'s configured non-root
    /// user before anything else mounts them — Docker creates fresh named volumes as
    /// <c>root:root</c>, which breaks images that run as a non-root user (e.g. n8n's <c>node</c>
    /// user). Existing volumes are left untouched. Best-effort: failures are logged, never thrown,
    /// so a volume that can't be fixed doesn't block deployment outright.
    /// </summary>
    Task EnsureNamedVolumesReadyAsync(string image, IEnumerable<Mount> mounts, CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="command"/> via <c>docker exec</c> inside the container labeled with
    /// <paramref name="serviceId"/>'s id label (through <c>/bin/sh -c</c>), waiting up to
    /// <paramref name="timeout"/> for it to finish. Fails with <see cref="Error.Docker"/>.ContainerNotFound
    /// when no container exists for the service.
    /// </summary>
    Task<Result<(long ExitCode, string StdOut, string StdErr)>> ExecInContainerByServiceIdAsync(
        Guid serviceId, string command, TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Builds the <see cref="CreateContainerParameters"/> for <paramref name="service"/>: env vars
    /// (plus feature flags), volume mounts, merged Traefik labels, and — when
    /// <paramref name="ensureNamedVolumesReady"/> is set — named-volume ownership fixups. Joins the
    /// container to its Project/Environment network at creation time (via
    /// <paramref name="networkingService"/>) so Traefik never observes it on the default bridge
    /// network mid-transition; the resolved network's name is returned for later IP extraction via
    /// <see cref="BuildServiceDeployData"/>.
    /// </summary>
    Task<(CreateContainerParameters Param, string? EnvironmentNetworkName)> BuildServiceContainerParametersAsync(
        Service service,
        string image,
        IReadOnlyList<string> ports,
        IReadOnlyList<string> commandArgs,
        RestartPolicy restartPolicy,
        bool ensureNamedVolumesReady,
        INetworkingService networkingService,
        CancellationToken cancellationToken);

    /// <summary>
    /// Connects a freshly-created container to any Shared/External networks already assigned to
    /// <paramref name="service"/> (its Project/Environment network is already attached at creation
    /// time by <see cref="BuildServiceContainerParametersAsync"/>).
    /// </summary>
    Task ConnectServiceToAssignedNetworksAsync(Service service, INetworkingService networkingService, CancellationToken cancellationToken);

    /// <summary>Extracts <see cref="DeployData"/> from an inspected container, preferring the Project/Environment network's IP.</summary>
    DeployData BuildServiceDeployData(Service service, string containerName, ContainerInspectResponse inspect, string? environmentNetworkName);

    /// <summary>Best-effort verifies/heals Traefik routing for the deployed service; failures are logged, never thrown.</summary>
    Task HealTraefikRoutingBestEffortAsync(Guid serviceId, string? expectedIpAddress, CancellationToken cancellationToken);
}