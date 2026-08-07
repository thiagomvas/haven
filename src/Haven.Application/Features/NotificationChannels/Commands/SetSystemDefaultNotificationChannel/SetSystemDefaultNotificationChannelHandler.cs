using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels.Commands.SetSystemDefaultNotificationChannel;

public class SetSystemDefaultNotificationChannelHandler(INotificationChannelConfigRepository repository)
    : ICommandHandler<SetSystemDefaultNotificationChannelCommand>
{
    public async ValueTask<Result> Handle(SetSystemDefaultNotificationChannelCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(NotificationChannelConfig), command.Id);

        if (config.Channel != NotificationChannel.Smtp)
            return Error.Validation("Only SMTP channels can be marked as the system default.");

        var previousDefault = await repository.GetSystemDefaultAsync(config.Channel, cancellationToken);
        if (previousDefault is not null && previousDefault.Id != config.Id)
        {
            previousDefault.ClearSystemDefault();
            await repository.UpdateAsync(previousDefault, cancellationToken);
        }

        config.SetAsSystemDefault();
        await repository.UpdateAsync(config, cancellationToken);

        return Result.Success();
    }
}
