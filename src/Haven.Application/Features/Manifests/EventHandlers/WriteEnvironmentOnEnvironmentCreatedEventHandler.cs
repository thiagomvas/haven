using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Entities;
using Haven.Domain.Events;

using Mediator;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteEnvironmentOnEnvironmentCreatedEventHandler(IManifestSerializer<Environment> serializer, IEnvironmentRepository repository) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is not null)
        {
            await serializer.WriteAsync(environment, cancellationToken);
        }
    }
}