using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetDashboardOverviewQuery : IQuery<DashboardOverviewDto>;