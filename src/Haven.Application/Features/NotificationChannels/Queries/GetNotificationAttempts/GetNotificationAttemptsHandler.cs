using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Queries.GetNotificationAttempts;

public class GetNotificationAttemptsHandler(INotificationAttemptRepository repository)
    : IPagedQueryHandler<GetNotificationAttemptsQuery, NotificationAttemptDto>
{
    public async ValueTask<PagedResult<NotificationAttemptDto>> Handle(
        GetNotificationAttemptsQuery query, CancellationToken cancellationToken)
    {
        var result = await repository.GetPagedByChannelConfigIdAsync(
            query.ChannelConfigId, query.PageNumber, query.PageSize, cancellationToken);
        return result.Project(a => new NotificationAttemptDto(
            a.Id,
            a.ChannelConfigId,
            a.Rule?.ChannelConfig?.Name ?? string.Empty,
            a.Channel,
            a.EventType,
            a.Status,
            a.ErrorMessage,
            a.AttemptedAt));
    }
}