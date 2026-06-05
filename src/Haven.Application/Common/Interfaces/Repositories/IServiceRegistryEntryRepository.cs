using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRegistryEntryRepository
{
    Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default);
    Task InsertAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task UpdateAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
}