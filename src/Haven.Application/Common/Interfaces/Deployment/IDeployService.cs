using Haven.Application.Common.Contracts;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeployService
{
    ServiceType ServiceType { get; }
    Task<Result<DeployData>> DeployAsync(Service service, Guid deploymentId, CancellationToken cancellationToken);
    Task<Result> StopAsync(Service service, CancellationToken cancellationToken);
    Task<Result<DeployData>> StartAsync(Service service, CancellationToken cancellationToken);
    Task CleanupAsync(Service service, CancellationToken cancellationToken);
}