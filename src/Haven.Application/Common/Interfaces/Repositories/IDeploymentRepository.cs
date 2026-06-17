namespace Haven.Application.Common.Interfaces.Repositories;

public interface IDeploymentRepository
{
    Task<Domain.Entities.Deployment> FindByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Domain.Entities.Deployment deployment, CancellationToken ct);
    Task<List<Domain.Entities.Deployment>> GetAllForServiceAsync(Guid serviceId, CancellationToken ct);
    Task RemoveAsync(Guid deploymentId, CancellationToken ct);
}