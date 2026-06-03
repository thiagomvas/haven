using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

namespace Haven.Application.Features.Projects.Queries.GetProjectDashboard;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetProjectDashboardQuery : IQuery<ProjectDashboardDto>
{
    public Guid ProjectId { get; init; }
}
