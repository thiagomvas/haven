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

    public Task AddResultAsync(HealthCheckResult result, CancellationToken cancellationToken)
    {
        context.HealthCheckResults.Add(result);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<HealthCheckResult>> GetResultsAsync(Guid healthCheckId, int limit, CancellationToken cancellationToken)
    {
        return await context.HealthCheckResults
            .AsNoTracking()
            .Where(r => r.HealthCheckId == healthCheckId)
            .OrderByDescending(r => r.RanAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task TrimResultsAsync(Guid healthCheckId, int keep, CancellationToken cancellationToken)
    {
        var cutoff = await context.HealthCheckResults
            .Where(r => r.HealthCheckId == healthCheckId)
            .OrderByDescending(r => r.RanAt)
            .Skip(keep - 1)
            .Select(r => (DateTime?)r.RanAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (cutoff is null)
            return;

        await context.HealthCheckResults
            .Where(r => r.HealthCheckId == healthCheckId && r.RanAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
