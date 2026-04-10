using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Projects.Events;

public sealed class ProjectCreatedEventHandler(
    IManifestSerializer serializer,
    ILogger<ProjectCreatedEventHandler> logger) : INotificationHandler<ProjectCreatedEvent>
{
    public async ValueTask Handle(ProjectCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling ProjectCreatedEvent for project: {ProjectName}", notification.Project.Name);
        await serializer.WriteProjectAsync(notification.Project, cancellationToken);
    }
}