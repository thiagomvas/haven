using Docker.DotNet.Models;

using Haven.Application.Common;

namespace Haven.Infrastructure.Deployment.Docker;

/// <summary>
/// Low-level, dependency-free Docker container lookup/inspect/restart primitives, split out of
/// <see cref="IDockerContainerRuntime"/> so that services which only need to find or restart a
/// container by owner id (<see cref="Services.TraefikApiClient"/>, <see cref="TraefikRoutingHealer"/>)
/// don't have to depend on the full runtime - which itself depends on
/// <see cref="ITraefikRoutingHealer"/> via <see cref="IDockerContainerRuntime.HealTraefikRoutingBestEffortAsync"/>.
/// Depending on the full runtime from either of those two would create a circular dependency
/// (TraefikApiClient -> IDockerContainerRuntime -> ITraefikRoutingHealer -> ITraefikApiClient).
/// </summary>
public interface IDockerContainerInspector
{
    /// <summary>Lists all containers (running or not) carrying the given label.</summary>
    Task<IList<ContainerListResponse>> GetContainersByLabelAsync(KeyValuePair<string, string> label, CancellationToken cancellationToken);

    /// <summary>Finds the container labeled with <paramref name="serviceId"/>'s id label and inspects it. Fails with <see cref="Error.Docker"/>.ContainerNotFound when none exists.</summary>
    Task<Result<ContainerInspectResponse>> InspectByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken);

    /// <summary>Finds the container labeled with <paramref name="ownerId"/>'s id label and restarts it in place. Fails with <see cref="Error.Docker"/>.ContainerNotFound when none exists.</summary>
    Task<Result> RestartByServiceIdAsync(Guid ownerId, CancellationToken cancellationToken);
}