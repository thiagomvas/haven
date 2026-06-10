using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Configuration.Events;

using Mediator;

namespace Haven.Application.Features.Configuration.EventHandlers;

public sealed class WriteConfigurationOnUpdatedHandler(IConfigurationWriteScheduler scheduler)
    : INotificationHandler<ConfigurationUpdatedNotification>
{
    public ValueTask Handle(ConfigurationUpdatedNotification notification, CancellationToken cancellationToken)
    {
        scheduler.ScheduleWrite();
        return ValueTask.CompletedTask;
    }
}
