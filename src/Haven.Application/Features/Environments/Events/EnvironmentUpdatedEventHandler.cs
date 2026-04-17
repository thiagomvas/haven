using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class EnvironmentUpdatedEventHandler(
    IManifestSerializer serializer,
    ILogger<EnvironmentUpdatedEventHandler> logger) : INotificationHandler<EnvironmentUpdatedEvent>
{
    public async ValueTask Handle(EnvironmentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling EnvironmentUpdatedEvent for environment: {EnvironmentName}", notification.Environment.Name);

        if (notification.OldName != notification.Environment.Name)
            await serializer.DeleteEnvironmentAsync(notification.Project, notification.OldName, cancellationToken);

        await serializer.WriteEnvironmentAsync(notification.Project, notification.Environment, cancellationToken);
    }
}
