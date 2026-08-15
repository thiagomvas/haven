using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class SidecarRepository(HavenDbContext context) : ISidecarRepository
{
    public async Task<Sidecar?> GetByIdAsync(Guid sidecarId, CancellationToken cancellationToken)
    {
        return await context.Sidecars
            .Include(s => s.SidecarNetworks)
            .ThenInclude(sn => sn.Network)
            .FirstOrDefaultAsync(s => s.Id == sidecarId, cancellationToken);
    }

    public async Task<Sidecar?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await context.Sidecars
            .Include(s => s.SidecarNetworks)
            .ThenInclude(sn => sn.Network)
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Sidecar>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Sidecars
            .Include(s => s.SidecarNetworks)
            .ThenInclude(sn => sn.Network)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(Sidecar sidecar, CancellationToken cancellationToken)
    {
        context.Add(sidecar);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(Sidecar sidecar, CancellationToken cancellationToken)
    {
        context.Sidecars.Remove(sidecar);
        return Task.CompletedTask;
    }
}
