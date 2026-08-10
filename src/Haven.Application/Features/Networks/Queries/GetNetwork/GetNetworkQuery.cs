using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Networks.Queries.ListNetworks;

namespace Haven.Application.Features.Networks.Queries.GetNetwork;

[RequirePermission(Permissions.Dns.Read)]
public sealed class GetNetworkQuery : IQuery<NetworkDto>
{
    public Guid NetworkId { get; init; }
}
