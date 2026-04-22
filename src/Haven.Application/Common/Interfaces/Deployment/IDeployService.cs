using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IDeployService
{
    ServiceType  ServiceType { get; }
    Task<Result> DeployAsync(Service service, CancellationToken cancellationToken);
}