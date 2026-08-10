using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Networks.Queries.GetNetwork;

public sealed class GetNetworkHandler(INetworkRepository networkRepository)
    : IQueryHandler<GetNetworkQuery, NetworkDto>
{
    public async ValueTask<Result<NetworkDto>> Handle(GetNetworkQuery query, CancellationToken cancellationToken)
    {
        var network = await networkRepository.GetByIdAsync(query.NetworkId, cancellationToken);
        if (network is null)
            return Error.NotFoundFor(nameof(Network), query.NetworkId);

        return Result<NetworkDto>.Success(network.ToDto());
    }
}
