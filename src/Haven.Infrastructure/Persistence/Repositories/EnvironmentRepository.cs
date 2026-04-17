using Haven.Application.Common.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Infrastructure.Persistence.Repositories;

public class EnvironmentRepository(HavenDbContext context) : IEnvironmentRepository
{
    public async Task<IReadOnlyList<Environment>> GetByProjectIdAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        return project is null
            ? []
            : [.. project.Environments.OrderBy(e => e.Name)];
    }
}
