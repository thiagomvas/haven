using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class DeploymentRepository(HavenDbContext context) : IDeploymentRepository
{
    public async Task<Domain.Entities.Deployment?> FindByIdAsync(Guid id, CancellationToken ct)
        => await context.Deployments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Domain.Entities.Deployment deployment, CancellationToken ct)
        => await context.Deployments.AddAsync(deployment, ct);

    public async Task<List<Domain.Entities.Deployment>> GetAllForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        var deployments = await context.Deployments
            .Where(d => d.ServiceId == serviceId)
            .ToListAsync(ct);
        return [.. deployments.OrderByDescending(d => d.StartedAt)];
    }

    public async Task<Dictionary<Guid, DeploymentStatus>> GetActiveStatusesAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken ct)
    {
        if (serviceIds.Count == 0) return [];

        var rows = await context.Deployments
            .AsNoTracking()
            .Where(d => serviceIds.Contains(d.ServiceId)
                        && d.Status != DeploymentStatus.Succeeded
                        && d.Status != DeploymentStatus.Failed
                        && d.Status != DeploymentStatus.Cancelled)
            .Select(d => new { d.ServiceId, d.Status, d.StartedAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.ServiceId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(r => r.Status == DeploymentStatus.Queued)
                .ThenByDescending(r => r.StartedAt)
                .First().Status);
    }

    public async Task<HashSet<Guid>> GetServicesWithLatestFailedDeploymentAsync(IReadOnlyCollection<Guid> serviceIds, CancellationToken ct)
    {
        if (serviceIds.Count == 0) return [];

        var rows = await context.Deployments
            .AsNoTracking()
            .Where(d => serviceIds.Contains(d.ServiceId))
            .Select(d => new { d.ServiceId, d.Status, d.StartedAt })
            .ToListAsync(ct);

        return rows
            .GroupBy(d => d.ServiceId)
            .Select(g => g.OrderByDescending(d => d.StartedAt).First())
            .Where(d => d.Status == DeploymentStatus.Failed)
            .Select(d => d.ServiceId)
            .ToHashSet();
    }

    public async Task<List<Domain.Entities.Deployment>> GetRecentAsync(int limit, CancellationToken ct)
    {
        // Ordered in memory: SQLite cannot translate ORDER BY over DateTimeOffset.
        var rows = await context.Deployments
            .AsNoTracking()
            .Include(d => d.Service!)
                .ThenInclude(s => s.Environment!)
                    .ThenInclude(e => e.Project)
            .ToListAsync(ct);

        return [.. rows.OrderByDescending(d => d.StartedAt).Take(limit)];
    }

    public async Task<Domain.Entities.Deployment?> FindActiveForServiceAsync(Guid serviceId, CancellationToken ct)
    {
        var active = await context.Deployments
            .Where(d => d.ServiceId == serviceId
                        && d.Status != DeploymentStatus.Succeeded
                        && d.Status != DeploymentStatus.Failed
                        && d.Status != DeploymentStatus.Cancelled)
            .ToListAsync(ct);

        return active.OrderByDescending(d => d.StartedAt).FirstOrDefault();
    }

    public async Task RemoveAsync(Guid deploymentId, CancellationToken ct)
    {
        var deployment = await context.Deployments.FindAsync([deploymentId], ct);
        if (deployment is not null)
            context.Deployments.Remove(deployment);
    }

    public async Task<List<Domain.Entities.Deployment>> GetExcessDeploymentsAsync(int retentionCount, CancellationToken ct)
    {
        var serviceIds = await context.Deployments
            .Select(d => d.ServiceId)
            .Distinct()
            .ToListAsync(ct);

        var excess = new List<Domain.Entities.Deployment>();
        foreach (var serviceId in serviceIds)
        {
            var all = await context.Deployments
                .Where(d => d.ServiceId == serviceId)
                .ToListAsync(ct);
            excess.AddRange(all.OrderByDescending(d => d.StartedAt).Skip(retentionCount));
        }
        return excess;
    }

    public async Task<List<Guid>> FilterMissingIdsAsync(List<Guid> ids, CancellationToken ct)
    {
        var existingIds = await context.Deployments
            .Where(d => ids.Contains(d.Id))
            .Select(d => d.Id)
            .ToListAsync(ct);

        return ids.Except(existingIds).ToList();
    }
}