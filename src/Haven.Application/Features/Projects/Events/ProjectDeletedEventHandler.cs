using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Projects.Events;

public sealed class ProjectDeletedEventHandler(
    IManifestSerializer serializer,
    ILogger<ProjectDeletedEventHandler> logger) : INotificationHandler<ProjectDeletedEvent>
{
    public async ValueTask Handle(ProjectDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling ProjectDeletedEvent for project: {ProjectName}", notification.Project.Name);
        await serializer.DeleteProjectAsync(notification.Project, cancellationToken);
    }
}
