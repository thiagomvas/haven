using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Orchestrates the full lifecycle of any <see cref="IDeployableContainer"/> (<see cref="Service"/>
/// or <see cref="Sidecar"/>) — status transitions, metrics, and delegating the actual container
/// work to whichever <see cref="IDeployService"/> handles that container.
/// </summary>
public interface IDeploymentOrchestrator
{
    Task<Result> DeployAsync(IDeployableContainer container, CancellationToken cancellationToken);
    Task<Result> StopAsync(IDeployableContainer container, CancellationToken cancellationToken);
    Task<Result> StartAsync(IDeployableContainer container, CancellationToken cancellationToken);
    Task<Result> RestartAsync(IDeployableContainer container, CancellationToken cancellationToken);
}
