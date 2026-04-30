using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class DeleteManifestsOnEnvironmentDeletedEventHandler(
    IEnvironmentRepository repository,
    IManifestSerializer serializer) : INotificationHandler<EnvironmentDeletedEvent>
{
    public async ValueTask Handle(EnvironmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(notification.Id, cancellationToken);
        if (environment is null)
        {
            return;
        }
        await serializer.DeleteEnvironmentAsync(environment.Project, environment.Name, cancellationToken);
    }
}
