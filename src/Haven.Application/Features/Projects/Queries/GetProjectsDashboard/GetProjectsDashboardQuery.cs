using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
[RequirePermission(Permissions.ProjectManagement.Read)]

public sealed class GetProjectsDashboardQuery : PagedQuery<ProjectDashboardDto>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
