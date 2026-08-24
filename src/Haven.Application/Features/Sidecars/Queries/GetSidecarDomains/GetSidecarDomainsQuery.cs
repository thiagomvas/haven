using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

namespace Haven.Application.Features.Sidecars.Queries.GetSidecarDomains;

[RequirePermission(Permissions.Sidecars.Read)]
public sealed class GetSidecarDomainsQuery : IQuery<List<ServiceRegistryDomainDto>>
{
    public Guid SidecarId { get; set; }
}
