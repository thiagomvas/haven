using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;

[RequirePermission(Permissions.System.ManageNotifications)]
public class CreateNotificationChannelConfigCommand : ICommand<Guid>
{
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string ConfigJson { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}