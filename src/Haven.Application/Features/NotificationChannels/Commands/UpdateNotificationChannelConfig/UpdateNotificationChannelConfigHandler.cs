using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.NotificationChannels.Commands.UpdateNotificationChannelConfig;

public class UpdateNotificationChannelConfigHandler(INotificationChannelConfigRepository repository, IEncryptionService encryptionService)
    : ICommandHandler<UpdateNotificationChannelConfigCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateNotificationChannelConfigCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(config), command.Id);

        var configJson = config.Channel == NotificationChannel.Smtp
            ? SmtpConfigJsonCodec.MergePasswordOnUpdate(command.ConfigJson, config.Config, encryptionService)
            : command.ConfigJson;

        config.Update(command.Name, configJson, command.Enabled);
        await repository.UpdateAsync(config, cancellationToken);

        return Result<Guid>.Success(config.Id);
    }
}