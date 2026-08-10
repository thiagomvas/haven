using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Manifests.EventHandlers;

public sealed class WriteNetworkOnNetworkCreatedEventHandler(IManifestSerializer<Network> serializer, INetworkRepository repository)
    : INotificationHandler<NetworkCreatedEvent>
{
    public async ValueTask Handle(NetworkCreatedEvent notification, CancellationToken cancellationToken)
    {
        var network = await repository.GetByIdAsync(notification.NetworkId, cancellationToken);
        if (network is not null)
        {
            await serializer.WriteAsync(network, cancellationToken);
        }
    }
}