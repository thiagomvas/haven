using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetDeploymentLogs;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetDeploymentLogsQuery : IQuery<string[]>
{
    public Guid DeploymentId { get; init; }
}