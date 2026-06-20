using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class NetworkRepository(HavenDbContext context) : INetworkRepository
{
    public Task AddAsync(Network network, CancellationToken cancellationToken)
    {
        context.Networks.Add(network);
        return Task.CompletedTask;
    }

    public Task<Network?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => context.Networks
            .Include(n => n.ServiceNetworks)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public Task<IReadOnlyList<Network>> GetByProjectAndEnvironmentAsync(Guid projectId, Guid environmentId, CancellationToken cancellationToken)
        => context.Networks
            .Where(n => n.ProjectId == projectId && n.EnvironmentId == environmentId)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<Network>)t.Result.AsReadOnly(), cancellationToken);

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return context.Networks
            .Where(n => n.Id == id)
            .ExecuteDeleteAsync(cancellationToken)
            .ContinueWith(_ => Task.CompletedTask);
    }
}