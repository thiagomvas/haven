using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Queries.GetServicesByEnvironment;

[RequirePermission(Permissions.Services.View)]
public sealed class GetServicesByEnvironmentQuery : IQuery<IReadOnlyList<ServiceDto>>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
}
