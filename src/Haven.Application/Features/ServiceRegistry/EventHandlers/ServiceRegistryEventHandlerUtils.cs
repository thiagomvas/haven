using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;

namespace Haven.Application.Features.ServiceRegistry.EventHandlers;

public static class ServiceRegistryEventHandlerUtils
{
    public static async Task UpdateRegistryEntryAsync(Guid serviceId, IServiceRepository repository, IServiceRegistry registry,
        CancellationToken ct)
    {
        var service = await repository.GetByIdAsync(serviceId, ct);
        if (service is null) return;

        var entry = await registry.EnsureServiceRegisteredAsync(serviceId, ct);
        entry.UpdateFromService(service);
    }
}