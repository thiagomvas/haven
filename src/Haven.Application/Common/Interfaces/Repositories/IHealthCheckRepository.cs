using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IHealthCheckRepository
{
    Task<HealthCheck?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthCheck>> GetForServiceListAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthCheck>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(HealthCheck healthCheck, CancellationToken cancellationToken);
    Task RemoveAsync(HealthCheck healthCheck, CancellationToken cancellationToken);
}
