using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Events;

namespace Haven.Application.Features.NotificationRules.Queries.GetAllNotificationRules;

public class GetAllNotificationRulesHandler(INotificationRuleRepository repository)
    : IQueryHandler<GetAllNotificationRulesQuery, NotificationRuleEventConfigDto[]>
{
    public async ValueTask<Result<NotificationRuleEventConfigDto[]>> Handle(GetAllNotificationRulesQuery query, CancellationToken cancellationToken)
    {
        Dictionary<string, IReadOnlyList<Guid>> effectiveRules;

        if (query.Scope.HasValue && query.ScopeId.HasValue)
        {
            // When the scope has been claimed (has any scoped rules), show only the scoped config.
            // Unconfigured events at a claimed scope are silenced — consistent with dispatch behavior.
            // When unclaimed, fall back to global so the user can see what is currently effective.
            bool claimed = await repository.HasAnyScopedRulesAsync(query.Scope.Value, query.ScopeId.Value, cancellationToken);
            effectiveRules = claimed
                ? await repository.GetAllScopedRulesAsync(query.Scope.Value, query.ScopeId.Value, cancellationToken)
                : await repository.GetAllGlobalRulesAsync(cancellationToken);
        }
        else
        {
            effectiveRules = await repository.GetAllGlobalRulesAsync(cancellationToken);
        }

        var relevantTypes = DomainEvent.GetEventTypesForScope(query.Scope);
        var result = relevantTypes
            .Select(t => new NotificationRuleEventConfigDto(
                t.Name,
                effectiveRules.GetValueOrDefault(t.Name, [])))
            .ToArray();

        return Result<NotificationRuleEventConfigDto[]>.Success(result);
    }
}
