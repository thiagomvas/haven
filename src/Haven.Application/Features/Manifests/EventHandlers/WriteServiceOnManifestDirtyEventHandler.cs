using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteServiceOnManifestDirtyEventHandler(
    IManifestSerializer<Service> serializer,
    IServiceRepository serviceRepository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EntityType is not EntityType.Service) return;

        var service = await serviceRepository.GetByIdAsync(notification.EntityId!.Value, cancellationToken);
        if (service is null) return;

        await serializer.WriteAsync(service, cancellationToken);
    }
}