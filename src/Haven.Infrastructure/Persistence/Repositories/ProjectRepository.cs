using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;


namespace Haven.Infrastructure.Persistence.Repositories;

public class ProjectRepository(HavenDbContext context) : IProjectRepository, IFuzzySearchableRepository
{
    public Task<Guid> AddAsync(Project project, CancellationToken cancellationToken)
    {
        context.Projects.Add(project);
        return Task.FromResult(project.Id);
    }

    public async Task<Project?> GetByIdAsync(Guid projectId, CancellationToken cancellationToken)
        => await context.Projects
            .Include(p => p.Environments)
            .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken: cancellationToken);

    public async Task<Project?> FindByIdAsync(Guid projectId, CancellationToken cancellationToken)
        => await context.Projects.FindAsync([projectId], cancellationToken);

    public Task<Project?> GetByServiceIdAsync(Guid serviceId, CancellationToken cancellationToken)
        => context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .Where(p => p.Environments.Any(e => e.Services.Any(s => s.Id == serviceId)))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Project?> GetByIdWithEnvironmentsAsync(Guid projectId, CancellationToken cancellationToken)
        => context.Projects.Include(p => p.Environments).FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    public Task<Project?> GetByIdWithServicesAsync(Guid projectId, CancellationToken cancellationToken)
        => context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

    public Task<bool> ExistsWithNameAsync(string name, Guid excludeId, CancellationToken cancellationToken)
        => context.Projects.AnyAsync(p => p.Name == name && p.Id != excludeId, cancellationToken);

    public Task<PagedResult<Project>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
        => context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .OrderBy(p => p.Name)
            .ToPagedResultAsync(pageNumber, pageSize, cancellationToken);

    public void Remove(Project project) => context.Projects.Remove(project);
    public IAsyncEnumerable<Project> GetAsync(CancellationToken cancellationToken)
    {
        return context.Projects.AsAsyncEnumerable();
    }

    public async Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, CancellationToken cancellationToken)
    {
        var hits = await context.Projects.AsNoTracking()
            .Where(p => p.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(p => new FuzzySearchResult(
                "Project",
                p.Id,
                p.Name,
                1,
                null
            ))
            .ToListAsync(cancellationToken);

        return hits;
    }
}