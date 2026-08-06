using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;

namespace Haven.Application.Features.Networks.Queries.ListNetworks;

public sealed class ListNetworksHandler(INetworkRepository repository)
    : IPagedQueryHandler<ListNetworksQuery, NetworkDto>
{
    public async ValueTask<PagedResult<NetworkDto>> Handle(ListNetworksQuery query, CancellationToken cancellationToken)
    {
        var paged = await repository.GetPagedAsync(query.PageNumber, query.PageSize, query.Type, cancellationToken);
        return paged.Project(n => n.ToDto());
    }
}
