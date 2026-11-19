using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetRecentDeployments;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetRecentDeploymentsQuery : IQuery<List<RecentDeploymentDto>>
{
    /// <summary>Maximum number of deployments returned. Defaults to 5.</summary>
    public int Limit { get; init; } = 5;
}