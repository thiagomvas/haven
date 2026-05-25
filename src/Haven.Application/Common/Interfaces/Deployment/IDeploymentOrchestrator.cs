using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentOrchestrator
{
    Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken);
    Task<Result> StopServiceAsync(Service service, CancellationToken cancellationToken);
    Task<Result> StartServiceAsync(Service service, CancellationToken cancellationToken);
    Task<Result> RestartServiceAsync(Service service, CancellationToken cancellationToken);
}