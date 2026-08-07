using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceDeployedEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceDeployedEvent>
{
    public async ValueTask Handle(ServiceDeployedEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyStatusChangedAsync(notification.ServiceId, notification.Name, ServiceStatus.Running, cancellationToken);
    }
}