using Haven.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Persistence.Repositories;

public class EnvironmentRepository(HavenDbContext context) : IEnvironmentRepository
{
    public async Task<Environment?> GetByIdAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Environments.Any(e => e.Id == environmentId), cancellationToken);

        return project?.Environments.FirstOrDefault(e => e.Id == environmentId);
    }

    public async Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        return project is null
            ? []
            : [.. project.Environments.OrderBy(e => e.Name)];
    }
}
