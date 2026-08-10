using Haven.Application.Common.Interfaces;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Networks.EventHandlers;

public sealed class DeleteManifestOnNetworkDeletedHandler(IManifestSerializer<Network> serializer)
    : INotificationHandler<NetworkDeletedEvent>
{
    public async ValueTask Handle(NetworkDeletedEvent notification, CancellationToken cancellationToken)
    {
        var network = Network.Reconstitute(
            notification.NetworkId,
            notification.Name,
            notification.Type,
            metadata: null,
            projectId: null,
            environmentId: null,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow);

        await serializer.RemoveAsync(network, cancellationToken);
    }
}
