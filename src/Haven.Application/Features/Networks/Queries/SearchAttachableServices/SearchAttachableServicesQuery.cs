using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Networks.Queries.SearchAttachableServices;

[RequirePermission(Permissions.Dns.Read)]
public sealed class SearchAttachableServicesQuery : IQuery<List<AttachableServiceDto>>
{
    public Guid NetworkId { get; init; }
    public string? Search { get; init; }
    public int Count { get; init; } = 20;
}