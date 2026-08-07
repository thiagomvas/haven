using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

public sealed class NotifyOnServiceDeployingEventHandler(IServiceStatusNotifier notifier) : INotificationHandler<ServiceDeployingEvent>
{
    public async ValueTask Handle(ServiceDeployingEvent notification, CancellationToken cancellationToken)
    {
        await notifier.NotifyStatusChangedAsync(notification.ServiceId, notification.Name, ServiceStatus.Deploying, cancellationToken);
    }
}