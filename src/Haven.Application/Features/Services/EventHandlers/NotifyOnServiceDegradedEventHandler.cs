using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceDegradedEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceDegradedEvent>
{
    public async ValueTask Handle(ServiceDegradedEvent notification, CancellationToken cancellationToken)
    {
        var issue = notification.HealthCheckName is null
            ? null
            : new ServiceHealthIssueDto(notification.HealthCheckName, notification.Reason, notification.Message);

        await notifier.NotifyHealthChangedAsync(notification.ServiceId, notification.Name, ServiceHealth.Unhealthy, issue, cancellationToken);
    }
}
