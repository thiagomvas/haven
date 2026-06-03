using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.GetEnvironmentsByProject;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetEnvironmentsByProjectQuery : IQuery<IReadOnlyList<EnvironmentDto>>
{
    public Guid ProjectId { get; init; }
}
