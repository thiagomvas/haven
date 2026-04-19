using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Services.Events;

public sealed class ServiceCreatedEventHandler(
    IManifestSerializer serializer,
    ILogger<ServiceCreatedEventHandler> logger) : INotificationHandler<ServiceCreatedEvent>
{
    public async ValueTask Handle(ServiceCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Handling ServiceCreatedEvent for service: {ServiceName}", notification.Service.Name);
        await serializer.WriteServiceAsync(notification.Project, notification.Environment, notification.Service, cancellationToken);
    }
}
