using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRuleSummary;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationRuleSummaryQuery : IQuery<NotificationRuleSummaryItemDto[]>;
