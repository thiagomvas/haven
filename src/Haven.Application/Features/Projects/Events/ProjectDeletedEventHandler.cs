using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Projects.Events;

public sealed class ProjectDeletedEventHandler(
    IProjectRepository repository,
    IManifestSerializer serializer) : INotificationHandler<ProjectDeletedEvent>
{
    public async ValueTask Handle(ProjectDeletedEvent notification, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(notification.ProjectId, cancellationToken);
        if (project is null) return;
        await serializer.DeleteProjectAsync(project, cancellationToken);
    }
}
