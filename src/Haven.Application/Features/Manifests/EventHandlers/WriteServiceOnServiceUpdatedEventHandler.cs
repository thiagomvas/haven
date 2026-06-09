using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteServiceOnServiceUpdatedEventHandler(IManifestSerializer<Service> serializer, IServiceRepository repository) : INotificationHandler<ServiceUpdatedEvent>
{
    public async ValueTask Handle(ServiceUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service is not null)
        {
            if (notification.OldName != notification.NewName)
            {
                await serializer.RenameAsync(service, notification.OldName, notification.NewName, cancellationToken);
            }
            await serializer.WriteAsync(service, cancellationToken);
        }
    }
}