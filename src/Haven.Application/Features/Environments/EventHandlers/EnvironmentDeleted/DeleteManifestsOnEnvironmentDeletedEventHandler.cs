using Haven.Application.Common.Interfaces;
using Haven.Domain.Events;
using Mediator;
using Microsoft.Extensions.Logging;

namespace Haven.Application.Features.Environments.Events;

public sealed class DeleteManifestsOnEnvironmentDeletedEventHandler(
    IManifestSerializer serializer,
    ILogger<DeleteManifestsOnEnvironmentDeletedEventHandler> logger) : INotificationHandler<EnvironmentDeletedEvent>
{
    public async ValueTask Handle(EnvironmentDeletedEvent notification, CancellationToken cancellationToken)
    {
        await serializer.DeleteEnvironmentAsync(notification.Project, notification.Environment.Name, cancellationToken);
    }
}
