using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationRules.Commands.SetNotificationRulesForEvent;

public class SetNotificationRulesForEventHandler(INotificationRuleRepository repository)
    : ICommandHandler<SetNotificationRulesForEventCommand>
{
    public async ValueTask<Result> Handle(SetNotificationRulesForEventCommand command, CancellationToken cancellationToken)
    {
        await repository.SetGlobalRulesForEventAsync(command.EventType, command.ChannelIds, cancellationToken);
        return Result.Success();
    }
}
