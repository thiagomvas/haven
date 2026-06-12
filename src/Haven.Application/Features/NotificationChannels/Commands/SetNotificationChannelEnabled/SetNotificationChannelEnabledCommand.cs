using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.SetNotificationChannelEnabled;

[RequirePermission(Permissions.System.ManageNotifications)]
public class SetNotificationChannelEnabledCommand : ICommand
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; }
}
