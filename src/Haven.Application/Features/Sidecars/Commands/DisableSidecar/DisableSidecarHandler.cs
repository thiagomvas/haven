using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Sidecars.Commands.DisableSidecar;

public sealed class DisableSidecarHandler(
    ISidecarRepository sidecarRepository,
    IDeploymentJobEnqueuer deploymentJobEnqueuer)
    : ICommandHandler<DisableSidecarCommand>
{
    public async ValueTask<Result> Handle(DisableSidecarCommand request, CancellationToken cancellationToken)
    {
        var sidecar = await sidecarRepository.GetByIdAsync(request.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), request.SidecarId);

        if (!sidecar.Enabled)
            return Result.Success();

        var wasRunning = sidecar.Status != ServiceStatus.Stopped;

        sidecar.Disable();

        if (wasRunning)
            deploymentJobEnqueuer.EnqueueSidecarStop(sidecar.Id);

        return Result.Success();
    }
}
