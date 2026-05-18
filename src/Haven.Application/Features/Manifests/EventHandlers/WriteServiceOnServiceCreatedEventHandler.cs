using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteServiceOnServiceCreatedEventHandler(IManifestSerializer<Service> serializer, IServiceRepository repository) : INotificationHandler<ServiceCreatedEvent>
{
    public async ValueTask Handle(ServiceCreatedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service is not null)
        {
            await serializer.WriteAsync(service, cancellationToken);
        }
    }
}
