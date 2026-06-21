using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceRegistryEntriesQuery : PagedQuery<ServiceRegistryEntryDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
}