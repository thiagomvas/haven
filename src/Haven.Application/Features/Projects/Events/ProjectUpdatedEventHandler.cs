using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Projects.Events;

public sealed class ProjectUpdatedEventHandler(
    IManifestSerializer serializer,
    ILogger<ProjectUpdatedEventHandler> logger) : INotificationHandler<ProjectUpdatedEvent>
{
    public async ValueTask Handle(ProjectUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling ProjectUpdatedEvent for project: {ProjectName}", notification.Project.Name);
        await serializer.WriteProjectAsync(notification.Project, cancellationToken);
    }
}
