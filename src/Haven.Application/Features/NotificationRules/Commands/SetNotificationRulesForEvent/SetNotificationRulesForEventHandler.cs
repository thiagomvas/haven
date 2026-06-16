using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;

public class SetNotificationRulesForEventHandler(INotificationRuleRepository repository)
    : ICommandHandler<SetNotificationRulesForEventCommand>
{
    public async ValueTask<Result> Handle(SetNotificationRulesForEventCommand command, CancellationToken cancellationToken)
    {
        if (command.Scope.HasValue && command.ScopeId.HasValue)
            await repository.SetScopedRulesForEventAsync(command.EventType, command.Scope.Value, command.ScopeId.Value, command.ChannelIds, cancellationToken);
        else
            await repository.SetGlobalRulesForEventAsync(command.EventType, command.ChannelIds, cancellationToken);

        return Result.Success();
    }
}