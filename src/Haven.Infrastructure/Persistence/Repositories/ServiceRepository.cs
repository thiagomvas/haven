using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository(HavenDbContext context) : IServiceRepository
{
    public async Task<Service?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Environments.Any(e => e.Services.Any(s => s.Id == serviceId)), cancellationToken);

        return project?.Environments
            .FirstOrDefault(e => e.Services.Any(s => s.Id == serviceId))?
            .Services
            .FirstOrDefault(s => s.Id == serviceId);
    }

    public async Task<IReadOnlyList<Service>> GetByEnvironmentIdAsync(Guid environmentId, CancellationToken cancellationToken)
    {
        var project = await context.Projects
            .Include(p => p.Environments)
                .ThenInclude(e => e.Services)
            .FirstOrDefaultAsync(p => p.Environments.Any(e => e.Id == environmentId), cancellationToken);

        if (project is null) return [];

        var environment = project.Environments.FirstOrDefault(e => e.Id == environmentId);
        return environment is null ? [] : [.. environment.Services.OrderBy(s => s.Name)];
    }

    public Task AddAsync(Service service, CancellationToken cancellationToken)
    {
        context.Add(service);
        return Task.CompletedTask;
    }
}
