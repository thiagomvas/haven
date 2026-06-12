using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfigs;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationChannelConfigsQuery : PagedQuery<NotificationChannelConfigDto>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
