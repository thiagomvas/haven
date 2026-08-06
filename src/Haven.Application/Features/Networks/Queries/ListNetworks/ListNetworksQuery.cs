using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Networks.Queries.ListNetworks;

[RequirePermission(Permissions.Dns.Read)]
public sealed class ListNetworksQuery : PagedQuery<NetworkDto>
{
    public NetworkType? Type { get; init; }
}
