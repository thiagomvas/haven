using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteEnvironmentOnEnvironmentDeletedEventHandler(IManifestSerializer<Environment> serializer, IEnvironmentRepository repository) : INotificationHandler<EnvironmentDeletedEvent>
{
    public async ValueTask Handle(EnvironmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is not null)
        {
            await serializer.RemoveAsync(environment, cancellationToken);
        }
    }
}
