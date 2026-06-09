using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

namespace Haven.Application.Features.Environments.Queries.GetEnvironmentDashboard;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetEnvironmentDashboardQuery : IQuery<EnvironmentDashboardDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
}