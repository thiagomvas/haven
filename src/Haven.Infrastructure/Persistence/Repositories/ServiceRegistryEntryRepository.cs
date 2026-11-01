using System.Linq.Expressions;

using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class ServiceRegistryEntryRepository(HavenDbContext db) : IServiceRegistryEntryRepository
{
    public Task<PagedResult<ServiceRegistryEntry>> GetPagedAsync(int pageNumber, int pageSize, ServiceRegistryEntryFilter? filter = null, string? sortBy = null, bool sortDescending = false, CancellationToken ct = default)
    {
        var query = db.ServiceRegistryEntries
            .AsNoTracking()
            .Include(e => e.Service!).ThenInclude(s => s.Environment!).ThenInclude(env => env.Project)
            .Include(e => e.Sidecar)
            .Include(e => e.Domains).ThenInclude(d => d.Certificate)
            .AsQueryable();

        if (filter is not null)
            query = ApplyFilter(query, filter);

        return ApplySort(query, sortBy, sortDescending).ToPagedResultAsync(pageNumber, pageSize, ct);
    }

    public Task<PagedResult<ServiceRegistryDomain>> GetDomainsPagedAsync(int pageNumber, int pageSize, string? search, string? sortBy = null, bool sortDescending = false, CancellationToken ct = default)
    {
        var query = db.ServiceRegistryDomains
            .AsNoTracking()
            .Include(d => d.Certificate)
            .Include(d => d.ServiceRegistryEntry!).ThenInclude(e => e.Service!).ThenInclude(s => s.Environment!).ThenInclude(env => env.Project)
            .Include(d => d.ServiceRegistryEntry!).ThenInclude(e => e.Sidecar)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.Hostname.Contains(search) ||
                (d.ServiceRegistryEntry!.ContainerName != null && d.ServiceRegistryEntry.ContainerName.Contains(search)) ||
                (d.ServiceRegistryEntry.Service != null && (
                    d.ServiceRegistryEntry.Service.Name.Contains(search) ||
                    d.ServiceRegistryEntry.Service.Environment!.Name.Contains(search) ||
                    d.ServiceRegistryEntry.Service.Environment.Project!.Name.Contains(search))) ||
                (d.ServiceRegistryEntry.Sidecar != null && d.ServiceRegistryEntry.Sidecar.Name.Contains(search)));
        }

        var ordered = (sortBy ?? string.Empty).ToLowerInvariant() switch
        {
            "tls" => Order(query, d => d.TlsMode, sortDescending),
            "service" => Order(query, d => d.ServiceRegistryEntry!.Service != null ? d.ServiceRegistryEntry.Service.Name : d.ServiceRegistryEntry.Sidecar!.Name, sortDescending),
            "created" => Order(query, d => d.CreatedAt, sortDescending),
            _ => Order(query, d => d.Hostname, sortDescending),
        };

        return ordered.ThenBy(d => d.Hostname).ThenBy(d => d.Id).ToPagedResultAsync(pageNumber, pageSize, ct);
    }

    public async Task<ServiceRegistrySummary> GetSummaryAsync(CancellationToken ct = default)
    {
        var entries = db.ServiceRegistryEntries.AsNoTracking();

        var byStatus = await entries
            .GroupBy(e => e.RuntimeState)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var running = byStatus.Where(x => x.Status == ServiceRuntimeState.Running).Sum(x => x.Count);
        var stopped = byStatus.Where(x => x.Status is ServiceRuntimeState.Stopped or ServiceRuntimeState.Crashed).Sum(x => x.Count);
        var starting = byStatus.Where(x => x.Status == ServiceRuntimeState.Restarting).Sum(x => x.Count);
        var unknown = byStatus.Where(x => x.Status is ServiceRuntimeState.Unknown or ServiceRuntimeState.NotDeployed).Sum(x => x.Count);
        var total = byStatus.Sum(x => x.Count);

        var domains = await db.ServiceRegistryDomains.AsNoTracking().CountAsync(ct);
        var external = await entries.CountAsync(e => e.Service != null && e.Service.ExposureMode == ExposureMode.External, ct);

        return new ServiceRegistrySummary(total, running, stopped, starting, unknown, domains, external);
    }

    private static IQueryable<ServiceRegistryEntry> ApplyFilter(IQueryable<ServiceRegistryEntry> query, ServiceRegistryEntryFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e =>
                (e.ContainerName != null && e.ContainerName.Contains(search)) ||
                (e.Sidecar != null && e.Sidecar.Name.Contains(search)) ||
                (e.Service != null && (
                    e.Service.Name.Contains(search) ||
                    e.Service.Environment!.Name.Contains(search) ||
                    e.Service.Environment.Project!.Name.Contains(search))) ||
                e.Domains.Any(d => d.Hostname.Contains(search)));
        }

        if (filter.Status is { } status)
        {
            query = status switch
            {
                RegistryStatusFilter.Running => query.Where(e => e.RuntimeState == ServiceRuntimeState.Running),
                RegistryStatusFilter.Stopped => query.Where(e => e.RuntimeState == ServiceRuntimeState.Stopped || e.RuntimeState == ServiceRuntimeState.Crashed),
                RegistryStatusFilter.Starting => query.Where(e => e.RuntimeState == ServiceRuntimeState.Restarting),
                _ => query.Where(e => e.RuntimeState == ServiceRuntimeState.Unknown || e.RuntimeState == ServiceRuntimeState.NotDeployed),
            };
        }

        if (filter.Exposure is { } exposure)
            query = query.Where(e => e.Service != null && e.Service.ExposureMode == exposure);

        if (filter.Type is { } type)
            query = query.Where(e => e.Service != null && e.Service.Type == type);

        if (filter.ProjectId is { } projectId)
            query = query.Where(e => e.Service != null && e.Service.Environment!.ProjectId == projectId);

        if (filter.EnvironmentId is { } environmentId)
            query = query.Where(e => e.Service != null && e.Service.EnvironmentId == environmentId);

        return query;
    }

    private static IOrderedQueryable<ServiceRegistryEntry> ApplySort(IQueryable<ServiceRegistryEntry> query, string? sortBy, bool descending)
    {
        // Unspecified sort keeps the historical default: most recently updated first.
        if (string.IsNullOrWhiteSpace(sortBy))
            return query.OrderByDescending(e => e.UpdatedAt);

        var ordered = sortBy.ToLowerInvariant() switch
        {
            "name" => Order(query, e => e.ContainerName ?? string.Empty, descending),
            "type" => Order(query, e => e.Service != null ? (ServiceType?)e.Service.Type : null, descending),
            "project" => Order(query, e => e.Service != null ? e.Service.Environment!.Project!.Name : string.Empty, descending),
            "exposure" => Order(query, e => e.Service != null ? (ExposureMode?)e.Service.ExposureMode : null, descending),
            "status" => Order(query, e => e.RuntimeState, descending),
            // Uptime grows as StartedAt gets older, so the key is inverted.
            "uptime" => Order(query, e => e.StartedAt, !descending),
            _ => Order(query, e => e.UpdatedAt, descending),
        };

        // Stable tiebreaker so pages don't shuffle between requests.
        return ordered.ThenBy(e => e.ContainerName).ThenBy(e => e.Id);
    }

    private static IOrderedQueryable<T> Order<T, TKey>(
        IQueryable<T> query, Expression<Func<T, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    public async Task<Dictionary<Guid, ServiceRegistryEntry>> GetByOwnerIdsAsync(IReadOnlyCollection<Guid> ownerIds, CancellationToken ct = default)
    {
        if (ownerIds.Count == 0) return [];

        var entries = await db.ServiceRegistryEntries
            .AsNoTracking()
            .Where(e => (e.ServiceId != null && ownerIds.Contains(e.ServiceId.Value))
                        || (e.SidecarId != null && ownerIds.Contains(e.SidecarId.Value)))
            .ToListAsync(ct);

        return entries.ToDictionary(e => e.OwnerId);
    }

    public async Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        var local = db.ServiceRegistryEntries.Local.FirstOrDefault(s => s.ServiceId == serviceId);
        if (local is not null) return local;

        return await db.ServiceRegistryEntries
            .Where(s => s.ServiceId == serviceId)
            .Include(s => s.Service)
            .Include(s => s.Domains).ThenInclude(d => d.Certificate)
            .OrderBy(s => s.RegisteredAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ServiceRegistryEntry?> GetForSidecarAsync(Guid sidecarId, CancellationToken ct = default)
    {
        var local = db.ServiceRegistryEntries.Local.FirstOrDefault(s => s.SidecarId == sidecarId);
        if (local is not null) return local;

        return await db.ServiceRegistryEntries
            .Where(s => s.SidecarId == sidecarId)
            .Include(s => s.Sidecar)
            .Include(s => s.Domains).ThenInclude(d => d.Certificate)
            .OrderBy(s => s.RegisteredAt)
            .FirstOrDefaultAsync(ct);
    }

    public Task InsertAsync(ServiceRegistryEntry entry, CancellationToken ct = default)
    {
        db.ServiceRegistryEntries.Add(entry);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceRegistryEntry entry, CancellationToken ct = default)
    {
        db.ServiceRegistryEntries.Update(entry);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ServiceRegistryEntry entry, CancellationToken ct = default)
    {
        db.ServiceRegistryEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public async Task<ServiceRegistryEntry?> GetByDomainIdAsync(Guid domainId, CancellationToken ct = default)
    {
        var local = db.ServiceRegistryEntries.Local.FirstOrDefault(e => e.Domains.Any(d => d.Id == domainId));
        if (local is not null) return local;

        return await db.ServiceRegistryEntries
            .Where(e => e.Domains.Any(d => d.Id == domainId))
            .Include(e => e.Domains).ThenInclude(d => d.Certificate)
            .SingleOrDefaultAsync(ct);
    }

    public Task<bool> HostnameExistsAsync(string hostname, Guid? excludingDomainId, CancellationToken ct = default)
    {
        var normalized = hostname.Trim().ToLowerInvariant();
        return db.ServiceRegistryDomains
            .AsNoTracking()
            .AnyAsync(d => d.Hostname == normalized && d.Id != excludingDomainId, ct);
    }

    public Task<List<Domain.Entities.ServiceRegistryDomain>> GetDomainsByCertificateIdAsync(Guid certificateId, CancellationToken ct = default) =>
        db.ServiceRegistryDomains
            .Where(d => d.SslCertificateId == certificateId)
            .ToListAsync(ct);
}