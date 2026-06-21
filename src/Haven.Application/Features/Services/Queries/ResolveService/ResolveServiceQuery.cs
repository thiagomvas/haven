using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.ResolveService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class ResolveServiceQuery : IQuery<ServiceLocationDto>
{
    public Guid ServiceId { get; init; }
}
