using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;
using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public class WriteProjectOnManifestDirtyEventHandler(IManifestSerializer<Project> serializer, IProjectRepository repository) : INotificationHandler<ManifestDirtyEvent>
{
    public async ValueTask Handle(ManifestDirtyEvent notification, CancellationToken cancellationToken)
    {
        await foreach (var project in repository.GetAsync(cancellationToken))
        {
            await serializer.WriteAsync(project, cancellationToken);
        }
    }
}