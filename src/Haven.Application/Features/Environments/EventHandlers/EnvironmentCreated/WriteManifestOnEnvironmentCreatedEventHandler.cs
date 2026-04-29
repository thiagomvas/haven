using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Environments.EventHandlers;

public class WriteManifestOnEnvironmentCreatedEventHandler(IManifestSerializer serializer) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        await serializer.WriteEnvironmentAsync(notification.Project, notification.Environment, cancellationToken);
    }
}