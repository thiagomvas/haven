using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;

[RequirePermission(Permissions.System.ManageNotifications)]
public class UpdateNotificationChannelConfigCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConfigJson { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
