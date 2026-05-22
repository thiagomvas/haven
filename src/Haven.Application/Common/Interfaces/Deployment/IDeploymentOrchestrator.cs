using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeploymentOrchestrator
{
    Task<Result> DeployServiceAsync(Service service, CancellationToken cancellationToken);
}