using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceStoppedEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceStoppedEvent>
{
    public async ValueTask Handle(ServiceStoppedEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyStatusChangedAsync(notification.ServiceId, notification.Name, ServiceStatus.Stopped, cancellationToken);
    }
}