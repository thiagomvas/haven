using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRuleSummary;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationRuleSummaryQuery : IQuery<NotificationRuleSummaryItemDto[]>
{
    public NotificationScope? Scope { get; set; }
    public Guid? ScopeId { get; set; }
}