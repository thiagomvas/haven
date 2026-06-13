using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Notifications;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Notifications.EventHandlers;

public class EnqueueNotificationOnDomainEventHandler(
    INotificationRuleRepository ruleRepository,
    INotificationEnqueuer enqueuer,
    IUnitOfWork uow) : INotificationHandler<DomainEvent>
{
    public async ValueTask Handle(DomainEvent notification, CancellationToken cancellationToken)
    {
        var rules = await ruleRepository.GetEnabledRulesForEventAsync(notification.GetType().Name, cancellationToken);
        foreach (var rule in rules)
            await enqueuer.EnqueueAsync(rule, notification, cancellationToken);
        
        await uow.SaveChangesAsync(cancellationToken);
    }
}