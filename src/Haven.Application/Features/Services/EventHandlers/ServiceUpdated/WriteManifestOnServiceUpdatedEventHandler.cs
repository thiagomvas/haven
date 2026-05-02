using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Services.EventHandlers.ServiceUpdated;

public sealed class WriteManifestOnServiceUpdatedEventHandler(
    IServiceRepository repository,
    IManifestSerializer serializer) : INotificationHandler<ServiceUpdatedEvent>
{
    public async ValueTask Handle(ServiceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service is null || service.Environment is null || service.Environment.Project is null)
            return;

        await serializer.WriteServiceAsync(service.Environment.Project, service.Environment, service, cancellationToken);
    }
}
