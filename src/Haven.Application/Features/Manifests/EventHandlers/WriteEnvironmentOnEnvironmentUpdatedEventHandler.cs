using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteEnvironmentOnEnvironmentUpdatedEventHandler(IManifestSerializer<Environment> serializer, IEnvironmentRepository repository) : INotificationHandler<EnvironmentUpdatedEvent>
{
    public async ValueTask Handle(EnvironmentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is not null)
        {
            if (notification.OldName != notification.NewName)
            {
                await serializer.RenameAsync(environment, notification.OldName, notification.NewName, cancellationToken);
            }
            await serializer.WriteAsync(environment, cancellationToken);
        }
    }
}
