using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Queries.GetDeploymentsForService;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetDeploymentsForServiceQuery : IQuery<List<DeploymentDto>>
{
    public Guid ProjectId { get; init; }
    public Guid EnvironmentId { get; init; }
    public Guid ServiceId { get; init; }
}
