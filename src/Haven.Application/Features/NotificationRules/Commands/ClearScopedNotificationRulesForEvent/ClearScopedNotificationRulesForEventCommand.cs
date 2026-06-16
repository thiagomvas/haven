using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.NotificationRules.Commands.ClearScopedNotificationRulesForEvent;

[RequirePermission(Permissions.System.ManageNotifications)]
public class ClearScopedNotificationRulesForEventCommand : ICommand
{
    public string EventType { get; set; } = string.Empty;
    public NotificationScope Scope { get; set; }
    public Guid ScopeId { get; set; }
}