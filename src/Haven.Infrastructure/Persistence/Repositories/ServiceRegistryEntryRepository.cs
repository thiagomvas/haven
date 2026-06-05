using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace Haven.Infrastructure.Persistence.Repositories;

public class ServiceRegistryEntryRepository(HavenDbContext db) : IServiceRegistryEntryRepository
{
    public async Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
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