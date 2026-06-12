using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;

[RequirePermission(Permissions.System.ManageNotifications)]
public class SetNotificationRulesForEventCommand : ICommand
{
    public string EventType { get; set; } = string.Empty;
    public IReadOnlyList<Guid> ChannelIds { get; set; } = [];
}
