using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

namespace Haven.Infrastructure.Persistence.Repositories;

public class ProjectRepository(HavenDbContext context) : IProjectRepository
{
    public async Task<Guid> AddAsync(Project project, CancellationToken cancellationToken)
    {
        context.Projects.Add(project);
        return project.Id;
    }
}