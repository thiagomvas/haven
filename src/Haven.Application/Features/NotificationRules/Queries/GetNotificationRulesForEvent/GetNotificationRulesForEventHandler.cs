using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRulesForEvent;

public class GetNotificationRulesForEventHandler(INotificationRuleRepository repository)
    : IQueryHandler<GetNotificationRulesForEventQuery, NotificationRuleEventConfigDto>
{
    public async ValueTask<Result<NotificationRuleEventConfigDto>> Handle(GetNotificationRulesForEventQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> channelIds;

        if (query.Scope.HasValue && query.ScopeId.HasValue)
            channelIds = await repository.GetChannelIdsForScopedEventAsync(query.EventType, query.Scope.Value, query.ScopeId.Value, cancellationToken);
        else
            channelIds = await repository.GetChannelIdsForEventAsync(query.EventType, cancellationToken);

        return Result<NotificationRuleEventConfigDto>.Success(new NotificationRuleEventConfigDto(query.EventType, channelIds));
    }
}