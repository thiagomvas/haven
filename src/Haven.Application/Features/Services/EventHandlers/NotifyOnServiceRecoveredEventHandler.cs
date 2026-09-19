using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceRecoveredEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceRecoveredEvent>
{
    public async ValueTask Handle(ServiceRecoveredEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyHealthChangedAsync(notification.ServiceId, notification.Name, ServiceHealth.Healthy, null, cancellationToken);
    }
}
