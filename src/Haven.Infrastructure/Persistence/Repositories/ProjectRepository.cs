using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class ProjectRepository(HavenDbContext context) : IProjectRepository
{
    public Task<Guid> AddAsync(Project project, CancellationToken cancellationToken)
    {
        context.Projects.Add(project);
        return Task.FromResult(project.Id);
    }

    public Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken)
        => context.Projects.FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    public Task<bool> ExistsWithNameAsync(string name, Guid excludeId, CancellationToken cancellationToken)
        => context.Projects.AnyAsync(p => p.Name == name && p.Id != excludeId, cancellationToken);

    public void Remove(Project project) => context.Projects.Remove(project);
}