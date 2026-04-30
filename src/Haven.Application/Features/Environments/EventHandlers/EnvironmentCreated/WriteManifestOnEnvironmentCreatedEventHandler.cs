using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Environments.EventHandlers;

public class WriteManifestOnEnvironmentCreatedEventHandler(
    IEnvironmentRepository repository,
    IManifestSerializer serializer) : INotificationHandler<EnvironmentCreatedEvent>
{
    public async ValueTask Handle(EnvironmentCreatedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is null)
        {
            return;
        }

        await serializer.WriteEnvironmentAsync(environment.Project, environment, cancellationToken);
    }
}