using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceRestartedEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceRestartedEvent>
{
    public async ValueTask Handle(ServiceRestartedEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyStatusChangedAsync(notification.ServiceId, notification.Name, ServiceStatus.Running, cancellationToken);
    }
}