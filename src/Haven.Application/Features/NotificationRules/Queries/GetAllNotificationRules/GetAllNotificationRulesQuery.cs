using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Queries.GetAllNotificationRules;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetAllNotificationRulesQuery : IQuery<NotificationRuleEventConfigDto[]>;
