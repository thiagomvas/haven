using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.GetEnvironmentsByProject;

[RequirePermission(Permissions.Environments.View)]
public sealed class GetEnvironmentsByProjectQuery : IQuery<IReadOnlyList<EnvironmentDto>>
{
    public Guid ProjectId { get; init; }
}
