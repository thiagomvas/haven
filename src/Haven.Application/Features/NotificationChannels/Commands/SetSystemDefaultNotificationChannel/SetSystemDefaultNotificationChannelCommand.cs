using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.SetSystemDefaultNotificationChannel;

[RequirePermission(Permissions.System.ManageNotifications)]
public sealed class SetSystemDefaultNotificationChannelCommand : ICommand
{
    public Guid Id { get; set; }
}