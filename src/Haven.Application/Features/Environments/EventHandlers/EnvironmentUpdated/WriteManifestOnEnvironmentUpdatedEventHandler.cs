using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class WriteManifestOnEnvironmentUpdatedEventHandler(
    IEnvironmentRepository repository,
    IManifestSerializer serializer) : INotificationHandler<EnvironmentUpdatedEvent>
{
    public async ValueTask Handle(EnvironmentUpdatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.OldName == notification.NewName)
        {
            return;
        }
        var environment = await repository.GetByIdAsync(notification.EnvironmentId, cancellationToken);
        if (environment is null || environment.Project is null) return;

        if (!string.IsNullOrWhiteSpace(notification.OldName) && !string.IsNullOrWhiteSpace(notification.NewName)
            && notification.OldName != notification.NewName)
        {
            await serializer.RenameEnvironmentAsync(environment.Project, notification.OldName, notification.NewName, cancellationToken);
        }
        
        await serializer.WriteEnvironmentAsync(environment.Project, environment, cancellationToken);
    }
}