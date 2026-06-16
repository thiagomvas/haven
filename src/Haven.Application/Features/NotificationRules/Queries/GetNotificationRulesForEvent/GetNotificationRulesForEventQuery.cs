using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRulesForEvent;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationRulesForEventQuery : IQuery<NotificationRuleEventConfigDto>
{
    public string EventType { get; set; } = string.Empty;
    public NotificationScope? Scope { get; set; }
    public Guid? ScopeId { get; set; }
}