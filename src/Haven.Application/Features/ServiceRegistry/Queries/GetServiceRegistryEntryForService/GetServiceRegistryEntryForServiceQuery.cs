using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntryForService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceRegistryEntryForServiceQuery : IQuery<ServiceRegistryEntryDto?>
{
    public Guid ServiceId { get; set; }
}