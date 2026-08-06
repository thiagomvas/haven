using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Networks.Queries.ListNetworks;

[RequirePermission(Permissions.Dns.Read)]
public sealed class ListNetworksQuery : PagedQuery<NetworkDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public NetworkType? Type { get; init; }
}
