using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class WriteManifestOnEnvironmentUpdatedEventHandler(
    IManifestSerializer serializer,
    ILogger<WriteManifestOnEnvironmentUpdatedEventHandler> logger) : INotificationHandler<EnvironmentUpdatedEvent>
{
    public async ValueTask Handle(EnvironmentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling EnvironmentUpdatedEvent for environment: {EnvironmentName}",
            notification.Environment.Name);

        await serializer.RenameEnvironmentAsync(notification.Project, notification.OldName,
            notification.Environment.Name, cancellationToken);
    }
}