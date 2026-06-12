using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.NotificationChannels;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationChannelConfig;

[RequirePermission(Permissions.System.ReadNotifications)]
public class GetNotificationChannelConfigQuery : IQuery<NotificationChannelConfigDto>
{
    public Guid Id { get; set; }
}
