using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteProjectOnProjectDeletedEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ProjectDeletedEvent>
{
    public async ValueTask Handle(ProjectDeletedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project is not null)
        {
            await serializer.RemoveAsync(project, cancellationToken);
        }
    }
}