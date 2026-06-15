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
        if (query.Scope.HasValue && query.ScopeId.HasValue)
        {
            var scopedCounts = await repository.GetScopedRuleCountsByEventTypeAsync(query.Scope.Value, query.ScopeId.Value, cancellationToken);
            var globalCounts = await repository.GetGlobalRuleCountsByEventTypeAsync(cancellationToken);

            var relevantTypes = DomainEvent.GetEventTypesForScope(query.Scope);
            var summary = relevantTypes
                .Select(t => new NotificationRuleSummaryItemDto(
                    t.Name,
                    DomainEvent.GetI18NKey(t),
                    scopedCounts.GetValueOrDefault(t.Name, 0),
                    IsOverridden: scopedCounts.ContainsKey(t.Name),
                    GlobalRuleCount: globalCounts.GetValueOrDefault(t.Name, 0)))
                .OrderBy(x => x.Name)
                .ToArray();

            return Result<NotificationRuleSummaryItemDto[]>.Success(summary);
        }
        else
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
}
