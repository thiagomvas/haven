using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

namespace Haven.Application.Features.Services.Queries.GetServiceDashboard;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetServiceDashboardQuery : IQuery<ServiceDashboardDto>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ServiceId { get; init; }
}
