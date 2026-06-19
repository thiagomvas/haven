using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Deployments.Commands.CancelDeployment;

public sealed class CancelDeploymentHandler(
    IDeploymentRepository deploymentRepository,
    IDeploymentCancellationService cancellationService)
    : ICommandHandler<CancelDeploymentCommand>
{
    public async ValueTask<Result> Handle(CancelDeploymentCommand request, CancellationToken cancellationToken)
    {
        var deployment = await deploymentRepository.FindByIdAsync(request.DeploymentId, cancellationToken);
        if (deployment is null)
            return Error.NotFoundFor("Deployment", request.DeploymentId);

        if (deployment.Status != DeploymentStatus.InProgress)
            return Error.Failure("Deployment.NotInProgress", "Only in-progress deployments can be cancelled.");

        cancellationService.Cancel(deployment.ServiceId);

        return Result.Success();
    }
}
