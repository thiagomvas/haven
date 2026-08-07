using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfig;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels.Commands.TestNotificationChannelConfigInline;

[RequirePermission(Permissions.System.ManageNotifications)]
public sealed class TestNotificationChannelConfigInlineCommand : ICommand<TestNotificationChannelConfigResult>
{
    public NotificationChannel Channel { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
}