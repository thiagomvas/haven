using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Queries.SearchAttachableServices;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository(HavenDbContext context) : IServiceRepository, IFuzzySearchableRepository
{
    public async Task<Service?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await context.Services
            .Include(s => s.Environment)
            .ThenInclude(e => e.Project)
            .Include(s => s.FeatureFlags)
            .Include(s => s.ServiceNetworks)
            .ThenInclude(sn => sn.Network)
            .Include(s => s.Volumes)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
    }

    public async Task<Service?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return await context.Services
            .Include(s => s.Environment)
            .ThenInclude(e => e.Project)
            .Include(s => s.Volumes)
            .FirstOrDefaultAsync(s => s.Token == token, cancellationToken);
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

    public IAsyncEnumerable<Service> GetAsync(CancellationToken cancellationToken)
    {
        return context.Services.AsAsyncEnumerable();
    }

    public Task RemoveAsync(Service service, CancellationToken cancellationToken)
    {
        context.Services.Remove(service);
        return Task.CompletedTask;
    }

    public async Task<List<Guid>> FilterMissingIdsAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        var existingIds = await context.Services
            .Where(s => ids.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return ids.Except(existingIds).ToList();
    }

    public async Task<List<AttachableServiceDto>> SearchAttachableAsync(
        Guid excludeNetworkId,
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = context.Services.AsNoTracking()
            .Where(s => !s.ServiceNetworks.Any(sn => sn.NetworkId == excludeNetworkId));

        var normalizedSearch = search?.Trim().ToLower();
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            query = query.Where(s =>
                s.Name.ToLower().Contains(normalizedSearch) ||
                s.Environment!.Name.ToLower().Contains(normalizedSearch) ||
                s.Environment!.Project!.Name.ToLower().Contains(normalizedSearch));
        }

        return await query
            .OrderBy(s => s.Environment!.Project!.Name)
            .ThenBy(s => s.Environment!.Name)
            .ThenBy(s => s.Name)
            .Take(limit)
            .Select(s => new AttachableServiceDto
            {
                Id = s.Id,
                Name = s.Name,
                Status = s.Status.ToString(),
                ProjectId = s.Environment!.Project!.Id,
                ProjectName = s.Environment!.Project!.Name,
                EnvironmentId = s.Environment!.Id,
                EnvironmentName = s.Environment!.Name
            })
            .ToListAsync(cancellationToken);
    }

    public string EntityType => nameof(Service);

    public async Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query.ToLower();
        var rows = await context.Projects.AsNoTracking()
            .SelectMany(p => p.Environments, (p, e) => new { ProjectId = p.Id, e })
            .SelectMany(x => x.e.Services, (x, s) => new { x.ProjectId, EnvironmentId = x.e.Id, Service = s })
            .Where(x => x.Service.Name.ToLower().Contains(normalizedQuery))
            .Select(x => new { x.ProjectId, x.EnvironmentId, x.Service.Id, x.Service.Name })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new FuzzySearchResult(
            "Service",
            x.Id,
            x.Name,
            1,
            new Dictionary<string, string>
            {
                ["projectId"] = x.ProjectId.ToString(),
                ["environmentId"] = x.EnvironmentId.ToString()
            }));
    }
}