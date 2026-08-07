using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.NotificationChannels.Commands.CreateNotificationChannelConfig;

public class CreateNotificationChannelConfigHandler(INotificationChannelConfigRepository repository, IEncryptionService encryptionService)
    : ICommandHandler<CreateNotificationChannelConfigCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateNotificationChannelConfigCommand command, CancellationToken cancellationToken)
    {
        var configJson = command.Channel == NotificationChannel.Smtp
            ? SmtpConfigJsonCodec.Encrypt(command.ConfigJson, encryptionService)
            : command.ConfigJson;

        var config = NotificationChannelConfig.Create(command.Name, command.Channel, configJson, command.Enabled);
        var id = await repository.AddAsync(config, cancellationToken);
        return Result<Guid>.CreatedFor(id);
    }
}