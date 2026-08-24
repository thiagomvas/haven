using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Common.Interfaces.Repositories;

public interface IServiceRegistryEntryRepository
{
    Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default);
    Task<ServiceRegistryEntry?> GetForSidecarAsync(Guid sidecarId, CancellationToken ct = default);

    /// <summary>Looks up the owning entry by one of its domains' id - domain ids are globally unique, so this needs no owner context.</summary>
    Task<ServiceRegistryEntry?> GetByDomainIdAsync(Guid domainId, CancellationToken ct = default);
    Task<PagedResult<ServiceRegistryEntry>> GetPagedAsync(int pageNumber, int pageSize, string? search, CancellationToken ct = default);
    Task InsertAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task UpdateAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task DeleteAsync(ServiceRegistryEntry entry, CancellationToken ct = default);
    Task<bool> HostnameExistsAsync(string hostname, Guid? excludingDomainId, CancellationToken ct = default);
}