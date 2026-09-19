using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IHealthCheckRepository : IRepository
{
    Task<HealthCheck?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthCheck>> GetForServiceListAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HealthCheck>> GetAllAsync(CancellationToken cancellationToken);

    Task AddAsync(HealthCheck healthCheck, CancellationToken cancellationToken);
    Task RemoveAsync(HealthCheck healthCheck, CancellationToken cancellationToken);

    Task AddResultAsync(HealthCheckResult result, CancellationToken cancellationToken);

    /// <summary>Returns the most recent results for a check, newest first.</summary>
    Task<IReadOnlyList<HealthCheckResult>> GetResultsAsync(Guid healthCheckId, int limit, CancellationToken cancellationToken);

    /// <summary>Deletes all but the <paramref name="keep"/> most recent results of a check.</summary>
    Task TrimResultsAsync(Guid healthCheckId, int keep, CancellationToken cancellationToken);
}
