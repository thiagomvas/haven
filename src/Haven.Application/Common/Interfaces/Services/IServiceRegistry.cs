using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Services;

public interface IServiceRegistry
{
    Task<ServiceRegistryEntry> EnsureServiceRegisteredAsync(Guid serviceId, CancellationToken ct = default);
    Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default);
}