using Haven.Application.Common.Contracts;
using Haven.Domain;
using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Deploys/stops/starts/cleans up a container-backed <see cref="IDeployableContainer"/>
/// (<see cref="Service"/> or <see cref="Sidecar"/>). Implementations declare which containers they
/// support via <see cref="CanHandle"/>; <see cref="IDeployServiceFactory"/> picks the first match.
/// </summary>
public interface IDeployService
{
    bool CanHandle(IDeployableContainer container);

    /// <summary>
    /// <paramref name="deploymentId"/> is the <c>Deployment</c> log row to append progress to, when
    /// one exists (only <see cref="Service"/> deployments are logged today; sidecars pass null).
    /// </summary>
    Task<Result<DeployData>> DeployAsync(IDeployableContainer container, Guid? deploymentId, CancellationToken cancellationToken);
    Task<Result> StopAsync(IDeployableContainer container, CancellationToken cancellationToken);
    Task<Result<DeployData>> StartAsync(IDeployableContainer container, CancellationToken cancellationToken);
    Task CleanupAsync(IDeployableContainer container, CancellationToken cancellationToken);
}