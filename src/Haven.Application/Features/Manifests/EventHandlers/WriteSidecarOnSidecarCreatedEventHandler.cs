using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteSidecarOnSidecarCreatedEventHandler(IManifestSerializer<Sidecar> serializer, ISidecarRepository repository)
    : INotificationHandler<SidecarCreatedEvent>
{
    public async ValueTask Handle(SidecarCreatedEvent notification, CancellationToken cancellationToken)
    {
        var sidecar = await repository.GetByIdAsync(notification.SidecarId, cancellationToken);
        if (sidecar is not null)
        {
            await serializer.WriteAsync(sidecar, cancellationToken);
        }
    }
}
