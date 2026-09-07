using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Aggregates;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Services;

public class ServiceRegistry(IServiceRegistryEntryRepository repository, ILogger<ServiceRegistry> logger) : IServiceRegistry
{
    public async Task<ServiceRegistryEntry> EnsureServiceRegisteredAsync(Guid serviceId, CancellationToken ct = default)
    {
        var existing = await GetForServiceAsync(serviceId, ct);
        if (existing is not null) return existing;

        var entry = ServiceRegistryEntry.Create(serviceId);
        await repository.InsertAsync(entry, ct);
        logger.LogInformation("Registered new service with ID {ServiceId} in the service registry", serviceId);
        return entry;
    }

    public async Task<ServiceRegistryEntry?> GetForServiceAsync(Guid serviceId, CancellationToken ct = default)
    {
        return await repository.GetForServiceAsync(serviceId, ct);
    }

    public async Task<ServiceRegistryEntry> EnsureSidecarRegisteredAsync(Guid sidecarId, CancellationToken ct = default)
    {
        var existing = await GetForSidecarAsync(sidecarId, ct);
        if (existing is not null) return existing;

        var entry = ServiceRegistryEntry.CreateForSidecar(sidecarId);
        await repository.InsertAsync(entry, ct);
        logger.LogInformation("Registered new sidecar with ID {SidecarId} in the service registry", sidecarId);
        return entry;
    }

    public async Task<ServiceRegistryEntry?> GetForSidecarAsync(Guid sidecarId, CancellationToken ct = default)
    {
        return await repository.GetForSidecarAsync(sidecarId, ct);
    }
}