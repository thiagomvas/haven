using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Sidecars.Commands.DisableSidecar;

public sealed class DisableSidecarHandler(
    ISidecarRepository sidecarRepository,
    IServiceRegistryEntryRepository registryEntryRepository,
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

        var entry = await registryEntryRepository.GetForSidecarAsync(sidecar.Id, cancellationToken);
        var wasRunning = entry?.RuntimeState is not (null or ServiceRuntimeState.NotDeployed or ServiceRuntimeState.Stopped);

        sidecar.Disable();

        if (wasRunning)
            await deploymentJobEnqueuer.EnqueueSidecarStop(sidecar.Id, cancellationToken);

        return Result.Success();
    }
}