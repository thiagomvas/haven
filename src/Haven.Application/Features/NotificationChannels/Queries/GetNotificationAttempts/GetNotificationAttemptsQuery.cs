using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationAttempts;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationAttemptsQuery : PagedQuery<NotificationAttemptDto>
{
    public Guid ChannelConfigId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}