using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.GetEnvironment;

[RequirePermission(Permissions.Environments.View)]
public sealed class GetEnvironmentQuery : IQuery<EnvironmentDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
}
