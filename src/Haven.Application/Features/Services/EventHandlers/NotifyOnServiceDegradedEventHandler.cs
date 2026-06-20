using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceDegradedEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceDegradedEvent>
{
    public async ValueTask Handle(ServiceDegradedEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyStatusChangedAsync(notification.ServiceId, notification.Name, ServiceStatus.Degraded, cancellationToken);
    }
}