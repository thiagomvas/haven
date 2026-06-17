using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Projects.EventHandlers;

public class DeleteManifestOnProjectDeletedHandler(IProjectRepository repository, IManifestSerializer<Project> serializer) : INotificationHandler<ProjectDeletedEvent>
{
    public async ValueTask Handle(ProjectDeletedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project == null) return;
        await serializer.RemoveAsync(project, cancellationToken);
    }
}