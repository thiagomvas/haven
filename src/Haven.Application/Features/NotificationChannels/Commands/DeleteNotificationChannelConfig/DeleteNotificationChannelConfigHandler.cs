using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Entities;

namespace Haven.Application.Features.NotificationChannels.Commands.DeleteNotificationChannelConfig;

public class DeleteNotificationChannelConfigHandler(INotificationChannelConfigRepository repository)
    : ICommandHandler<DeleteNotificationChannelConfigCommand>
{
    public async ValueTask<Result> Handle(DeleteNotificationChannelConfigCommand command, CancellationToken cancellationToken)
    {
        var config = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (config is null)
            return Error.NotFoundFor(nameof(NotificationChannelConfig), command.Id);

        repository.Remove(config);

        return Result.Success();
    }
}