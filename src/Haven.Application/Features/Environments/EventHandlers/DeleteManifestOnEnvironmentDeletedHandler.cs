using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;

using Mediator;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Environments.EventHandlers;

public class DeleteManifestOnEnvironmentDeletedHandler(IEnvironmentRepository repository, IManifestSerializer<Environment> serializer) : INotificationHandler<EnvironmentDeletedEvent>
{
    public async ValueTask Handle(EnvironmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        var env = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (env == null) return;
        await serializer.RemoveAsync(env, cancellationToken);
    }
}