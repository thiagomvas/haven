using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationRules.Queries.GetAllNotificationRules;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetAllNotificationRulesQuery : IQuery<NotificationRuleEventConfigDto[]>
{
    public NotificationScope? Scope { get; set; }
    public Guid? ScopeId { get; set; }
}