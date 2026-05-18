using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteServiceOnServiceDeletedEventHandler(IManifestSerializer<Service> serializer, IServiceRepository repository) : INotificationHandler<ServiceDeletedEvent>
{
    public async ValueTask Handle(ServiceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service is not null)
        {
            await serializer.RemoveAsync(service, cancellationToken);
        }
    }
}
