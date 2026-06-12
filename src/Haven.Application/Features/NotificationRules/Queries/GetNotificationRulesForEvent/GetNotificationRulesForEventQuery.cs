using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRulesForEvent;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationRulesForEventQuery : IQuery<NotificationRuleEventConfigDto>
{
    public string EventType { get; set; } = string.Empty;
}
