using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Networks.Queries.ListNetworks;

public sealed class ListNetworksHandler(INetworkRepository repository)
    : IQueryHandler<ListNetworksQuery, List<NetworkDto>>
{
    public async ValueTask<Result<List<NetworkDto>>> Handle(ListNetworksQuery query, CancellationToken cancellationToken)
    {
        var networks = await repository.GetAllAsync(query.Type, cancellationToken);
        return networks.Select(n => n.ToDto()).ToList();
    }
}
