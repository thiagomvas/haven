using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.DeleteNotificationChannelConfig;

[RequirePermission(Permissions.System.ManageNotifications)]
public class DeleteNotificationChannelConfigCommand : ICommand
{
    public Guid Id { get; set; }
}