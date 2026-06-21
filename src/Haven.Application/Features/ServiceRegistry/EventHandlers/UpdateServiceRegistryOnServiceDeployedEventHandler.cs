using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.ServiceRegistry.EventHandlers;

public class UpdateServiceRegistryOnServiceDeployedEventHandler(IServiceRegistry registry, IServiceRepository repository) : INotificationHandler<ServiceLifetimeDomainEvent>
{
    public async ValueTask Handle(ServiceLifetimeDomainEvent notification, CancellationToken cancellationToken)
    {
        await ServiceRegistryEventHandlerUtils.UpdateRegistryEntryAsync(notification.ServiceId, repository, registry, cancellationToken);
    }
}