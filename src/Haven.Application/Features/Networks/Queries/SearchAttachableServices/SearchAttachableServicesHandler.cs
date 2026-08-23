using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Networks.Queries.SearchAttachableServices;

public sealed class SearchAttachableServicesHandler(
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository)
    : IQueryHandler<SearchAttachableServicesQuery, List<AttachableServiceDto>>
{
    public async ValueTask<Result<List<AttachableServiceDto>>> Handle(
        SearchAttachableServicesQuery query,
        CancellationToken cancellationToken)
    {
        var network = await networkRepository.GetByIdAsync(query.NetworkId, cancellationToken);
        if (network is null)
            return Error.NotFoundFor(nameof(Network), query.NetworkId);

        var results = await serviceRepository.SearchAttachableAsync(
            query.NetworkId,
            query.Search,
            query.Count,
            cancellationToken);

        return results;
    }
}