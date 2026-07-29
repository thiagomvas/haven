using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class HealthCheckRepository(HavenDbContext context) : IHealthCheckRepository
{
    public async Task<HealthCheck?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.HealthChecks.FirstOrDefaultAsync(hc => hc.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<HealthCheck>> GetForServiceListAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await context.HealthChecks.Where(hc => hc.ServiceId == serviceId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HealthCheck>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.HealthChecks.ToListAsync(cancellationToken);
    }

    public Task AddAsync(HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        context.HealthChecks.Add(healthCheck);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(HealthCheck healthCheck, CancellationToken cancellationToken)
    {
        context.HealthChecks.Remove(healthCheck);
        return Task.CompletedTask;
    }
}