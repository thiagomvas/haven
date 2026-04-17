using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class EnvironmentCreatedEventHandler(
    IManifestSerializer serializer,
    ILogger<EnvironmentCreatedEventHandler> logger) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling EnvironmentCreatedEvent for environment: {EnvironmentName}", notification.Environment.Name);
        await serializer.WriteEnvironmentAsync(notification.Project, notification.Environment, cancellationToken);
    }
}
