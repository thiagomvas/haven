using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationAttempts;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationAttemptsQuery : PagedQuery<NotificationAttemptDto>
{
    public Guid? ChannelConfigId { get; set; }
}