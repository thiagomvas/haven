using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Notifications.EventHandlers;

public class EnqueueNotificationOnDomainEventHandler(
    INotificationRuleRepository ruleRepository,
    INotificationScopeResolver scopeResolver,
    INotificationEnqueuer enqueuer,
    IUnitOfWork uow) : INotificationHandler<DomainEvent>
{
    public async ValueTask Handle(DomainEvent notification, CancellationToken cancellationToken)
    {
        var eventName = notification.GetType().Name;
        IReadOnlyList<NotificationRule> rules = [];

        bool scopeOverrideFound = false;
        if (notification is IScopedDomainEvent scoped)
        {
            var chain = await scopeResolver.ResolveChainAsync(scoped.PrimaryScope, scoped.PrimaryScopeId, cancellationToken);
            foreach (var (scope, scopeId) in chain)
            {
                if (!await ruleRepository.HasAnyScopedRulesAsync(scope, scopeId, cancellationToken))
                    continue;

                rules = await ruleRepository.GetEnabledScopedRulesForEventAsync(eventName, scope, scopeId, cancellationToken);
                scopeOverrideFound = true;
                break;
            }
        }

        if (!scopeOverrideFound)
            rules = await ruleRepository.GetEnabledRulesForEventAsync(eventName, cancellationToken);

        foreach (var rule in rules)
            await enqueuer.EnqueueAsync(rule, notification, cancellationToken);

        await uow.SaveChangesAsync(cancellationToken);
    }
}
