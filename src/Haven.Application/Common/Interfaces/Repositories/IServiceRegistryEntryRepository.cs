using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRegistryEntryRepository
{
    Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default);
    Task<PagedResult<ServiceRegistryEntry>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
    Task InsertAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task UpdateAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task<bool> HostnameExistsAsync(string hostname, Guid? excludingDomainId, CancellationToken ct = default);
}