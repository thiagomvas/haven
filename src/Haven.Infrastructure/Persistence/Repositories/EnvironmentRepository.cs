using Haven.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Persistence.Repositories;

public class EnvironmentRepository(HavenDbContext context) : IEnvironmentRepository
{
    public async Task<Environment?> GetByIdAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        return await context.Environments.FindAsync([environmentId], cancellationToken);
    }

    public async Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId,
        CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        return project is null
            ? []
            : [.. project.Environments.OrderBy(e => e.Name)];
    }

    public Task AddAsync(Environment environment, CancellationToken cancellationToken)
    {
        context.Add(environment);
        return Task.CompletedTask;
    }
}