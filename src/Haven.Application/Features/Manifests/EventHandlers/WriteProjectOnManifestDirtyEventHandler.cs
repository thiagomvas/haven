using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteProjectOnManifestDirtyEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EntityType != EntityType.Project)
            return;

        var project = await repository.GetByIdAsync(notification.EntityId!.Value, cancellationToken);
        if (project is not null)
        {
            await serializer.WriteAsync(project, cancellationToken);
        }
    }
}