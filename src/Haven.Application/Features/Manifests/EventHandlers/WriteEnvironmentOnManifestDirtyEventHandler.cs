using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Events;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteEnvironmentOnManifestDirtyEventHandler(
    IManifestSerializer<Environment> serializer,
    IEnvironmentRepository environmentRepository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EntityType != EntityType.Environment)
            return;
        
        var environment = await environmentRepository.GetByIdAsync(notification.EntityId!.Value, cancellationToken);
        if (environment is null)
            return;
        await serializer.WriteAsync(environment, cancellationToken);
    }
}
