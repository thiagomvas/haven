using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Networks.Queries.ListNetworks;

[RequirePermission(Permissions.Dns.Read)]
public sealed class ListNetworksQuery : IQuery<List<NetworkDto>>
{
    public NetworkType? Type { get; init; }
}
