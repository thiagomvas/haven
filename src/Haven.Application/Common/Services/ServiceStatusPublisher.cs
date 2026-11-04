using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace Haven.Application.Common.Services;

public sealed class ServiceStatusPublisher(
    IServiceRepository serviceRepository,
    IServiceStateReader stateReader,
    IServiceStatusNotifier notifier,
    ILogger<ServiceStatusPublisher> logger) : IServiceStatusPublisher
{
    public async Task PublishAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service is null) return;

            var state = await stateReader.LoadAsync([serviceId], cancellationToken);
            await notifier.NotifyStatusChangedAsync(new ServiceStatusUpdate(
                serviceId,
                service.Name,
                state.GetDisplayStatus(serviceId),
                service.DesiredState,
                state.GetRuntimeState(serviceId),
                state.GetHealth(serviceId)), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish status for service {ServiceId}", serviceId);
        }
    }
}
