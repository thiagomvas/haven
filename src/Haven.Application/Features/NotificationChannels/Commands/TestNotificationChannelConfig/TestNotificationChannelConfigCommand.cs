using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;

[RequirePermission(Permissions.System.ManageNotifications)]
public sealed class TestNotificationChannelConfigCommand : ICommand<TestNotificationChannelConfigResult>
{
    public Guid Id { get; set; }
}
