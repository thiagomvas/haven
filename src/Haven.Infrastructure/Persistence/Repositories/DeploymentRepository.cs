using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;

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
}
