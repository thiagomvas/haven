using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteProjectOnProjectUpdatedEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ProjectUpdatedEvent>
{
    public async ValueTask Handle(ProjectUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project is not null)
        {
            if (notification.OldName != notification.NewName)
            {
                await serializer.RenameAsync(project, notification.OldName, notification.NewName, cancellationToken);
            }
            await serializer.WriteAsync(project, cancellationToken);
        }
    }
}