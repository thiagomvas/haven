using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class EnvironmentDeletedEventHandler(
    IManifestSerializer serializer,
    ILogger<EnvironmentDeletedEventHandler> logger) : INotificationHandler<EnvironmentDeletedEvent>
{
    public async ValueTask Handle(EnvironmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling EnvironmentDeletedEvent for environment: {EnvironmentName}", notification.Environment.Name);
        await serializer.DeleteEnvironmentAsync(notification.Project, notification.Environment.Name, cancellationToken);
    }
}
