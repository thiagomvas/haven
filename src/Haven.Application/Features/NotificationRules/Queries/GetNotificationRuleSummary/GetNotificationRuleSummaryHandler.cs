using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Events;

namespace Haven.Application.Features.NotificationRules.Queries.GetNotificationRuleSummary;

public class GetNotificationRuleSummaryHandler(INotificationRuleRepository repository)
    : IQueryHandler<GetNotificationRuleSummaryQuery, NotificationRuleSummaryItemDto[]>
{
    public async ValueTask<Result<NotificationRuleSummaryItemDto[]>> Handle(GetNotificationRuleSummaryQuery query, CancellationToken cancellationToken)
    {
        var counts = await repository.GetGlobalRuleCountsByEventTypeAsync(cancellationToken);

        var summary = DomainEvent.AllEventTypes
            .Select(t => new NotificationRuleSummaryItemDto(
                t.Name,
                DomainEvent.GetI18NKey(t),
                counts.GetValueOrDefault(t.Name, 0)))
            .OrderBy(x => x.Name)
            .ToArray();

        return Result<NotificationRuleSummaryItemDto[]>.Success(summary);
    }
}
