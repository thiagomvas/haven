using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;

[RequirePermission(Permissions.System.ManageNotifications)]
public class SetNotificationRulesForEventCommand : ICommand
{
    public string EventType { get; set; } = string.Empty;
    public IReadOnlyList<Guid> ChannelIds { get; set; } = [];
    public NotificationScope? Scope { get; set; }
    public Guid? ScopeId { get; set; }
}