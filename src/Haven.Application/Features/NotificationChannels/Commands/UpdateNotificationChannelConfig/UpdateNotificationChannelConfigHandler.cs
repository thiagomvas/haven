using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;

public class UpdateNotificationChannelConfigHandler(INotificationChannelConfigRepository repository)
    : ICommandHandler<UpdateNotificationChannelConfigCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateNotificationChannelConfigCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(config), command.Id);

        config.Update(command.Name, command.ConfigJson, command.Enabled);
        await repository.UpdateAsync(config, cancellationToken);

        return Result<Guid>.Success(config.Id);
    }
}