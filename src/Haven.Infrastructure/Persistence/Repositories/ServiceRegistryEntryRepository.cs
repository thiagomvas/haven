using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Infrastructure.Persistence.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class ServiceRegistryEntryRepository(HavenDbContext db) : IServiceRegistryEntryRepository
{
    public Task<PagedResult<ServiceRegistryEntry>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = db.ServiceRegistryEntries
            .AsNoTracking()
            .Include(e => e.Service)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.Service.Name.Contains(search) || e.ContainerName.Contains(search));
        }

        return query.OrderByDescending(e => e.UpdatedAt).ToPagedResultAsync(pageNumber, pageSize, ct);
    }

    public async Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        var local = db.ServiceRegistryEntries.Local.FirstOrDefault(s => s.ServiceId == serviceId);
        if (local is not null) return local;

        return await db.ServiceRegistryEntries
            .Where(s => s.ServiceId == serviceId)
            .Include(s => s.Service)
            .SingleOrDefaultAsync(ct);
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
}