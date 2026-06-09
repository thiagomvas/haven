using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class FeatureFlagRepository(HavenDbContext context) : IFeatureFlagRepository
{
    public async Task<PagedResult<FeatureFlag>> GetForServicePagedAsync(Guid serviceId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await context.FeatureFlags
            .Where(f => f.ServiceId == serviceId)
            .OrderBy(f => f.Name)
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public IAsyncEnumerable<FeatureFlag> GetForServiceAsync(Guid serviceId)
    {
        return context.FeatureFlags.Where(f => f.ServiceId == serviceId).AsAsyncEnumerable();
    }

    public async Task<FeatureFlag?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.FeatureFlags.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public async Task<List<FeatureFlag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        return await context.FeatureFlags.Where(f => ids.Contains(f.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FeatureFlag>> GetForServiceListAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await context.FeatureFlags.Where(f => f.ServiceId == serviceId).ToListAsync(cancellationToken);
    }

    public Task AddAsync(FeatureFlag featureFlag, CancellationToken cancellationToken)
    {
        context.FeatureFlags.Add(featureFlag);
        return Task.CompletedTask;
    }

    public Task AddAsync(IEnumerable<FeatureFlag> featureFlags, CancellationToken cancellationToken)
    {
        context.FeatureFlags.AddRange(featureFlags);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(FeatureFlag featureFlag, CancellationToken cancellationToken)
    {
        context.FeatureFlags.Remove(featureFlag);
        return Task.CompletedTask;
    }

    public async Task CleanForServiceAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var flags = await context.FeatureFlags.Where(f => f.ServiceId == serviceId).ToListAsync(cancellationToken);
        context.FeatureFlags.RemoveRange(flags);
    }
}