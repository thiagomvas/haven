using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.NotificationChannels.Commands.SetNotificationChannelEnabled;

public class SetNotificationChannelEnabledHandler(INotificationChannelConfigRepository repository)
    : ICommandHandler<SetNotificationChannelEnabledCommand>
{
    public async ValueTask<Result> Handle(SetNotificationChannelEnabledCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(NotificationChannelConfig), command.Id);

        config.SetEnabled(command.Enabled);
        await repository.UpdateAsync(config, cancellationToken);

        return Result.Success();
    }
}
