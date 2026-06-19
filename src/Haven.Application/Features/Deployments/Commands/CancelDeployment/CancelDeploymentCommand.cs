using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Deployments.Commands.CancelDeployment;

[RequirePermission(Permissions.ProjectManagement.ManageDeploys)]
public sealed class CancelDeploymentCommand : ICommand
{
    public Guid DeploymentId { get; set; }
}
