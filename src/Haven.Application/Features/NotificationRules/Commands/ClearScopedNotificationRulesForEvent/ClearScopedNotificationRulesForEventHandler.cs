using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Commands.ClearScopedNotificationRulesForEvent;

public class ClearScopedNotificationRulesForEventHandler(INotificationRuleRepository repository)
    : ICommandHandler<ClearScopedNotificationRulesForEventCommand>
{
    public async ValueTask<Result> Handle(ClearScopedNotificationRulesForEventCommand command, CancellationToken cancellationToken)
    {
        await repository.ClearScopedRulesForEventAsync(command.EventType, command.Scope, command.ScopeId, cancellationToken);
        return Result.Success();
    }
}