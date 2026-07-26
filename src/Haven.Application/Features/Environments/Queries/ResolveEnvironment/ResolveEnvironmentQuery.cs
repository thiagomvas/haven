using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Queries.ResolveEnvironment;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class ResolveEnvironmentQuery : IQuery<EnvironmentLocationDto>
{
    public Guid EnvironmentId { get; init; }
}
