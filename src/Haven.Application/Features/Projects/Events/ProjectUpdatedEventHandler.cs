using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
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

        if (notification.OldName != notification.Project.Name)
        {
            var stale = Project.Reconstitute(notification.Project.Id, notification.OldName, null);
            await serializer.DeleteProjectAsync(stale, cancellationToken);
        }

        await serializer.WriteProjectAsync(notification.Project, cancellationToken);
    }
}
