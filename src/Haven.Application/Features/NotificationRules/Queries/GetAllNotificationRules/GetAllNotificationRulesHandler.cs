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
        Dictionary<string, IReadOnlyList<Guid>> allRules;

        if (query.Scope.HasValue && query.ScopeId.HasValue)
            allRules = await repository.GetAllScopedRulesAsync(query.Scope.Value, query.ScopeId.Value, cancellationToken);
        else
            allRules = await repository.GetAllGlobalRulesAsync(cancellationToken);

        var relevantTypes = DomainEvent.GetEventTypesForScope(query.Scope);
        var result = relevantTypes
            .Select(t => new NotificationRuleEventConfigDto(
                t.Name,
                allRules.GetValueOrDefault(t.Name, [])))
            .ToArray();

        return Result<NotificationRuleEventConfigDto[]>.Success(result);
    }
}
