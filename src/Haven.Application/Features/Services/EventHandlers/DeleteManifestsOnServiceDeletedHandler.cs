using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public class DeleteManifestsOnServiceDeletedHandler(IServiceRepository repository, IManifestSerializer<Service> serializer) : INotificationHandler<ServiceDeletedEvent>
{
    public async ValueTask Handle(ServiceDeletedEvent notification, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(notification.ServiceId, cancellationToken);
        if (service == null) return;
        await serializer.RemoveAsync(service, cancellationToken);
    }
}