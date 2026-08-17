using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Sidecars.Commands.EnableSidecar;

public sealed class EnableSidecarHandler(
    ISidecarRepository sidecarRepository,
    IDeploymentJobEnqueuer deploymentJobEnqueuer,
    IHavenEnvironment havenEnvironment)
    : ICommandHandler<EnableSidecarCommand>
{
    public async ValueTask<Result> Handle(EnableSidecarCommand request, CancellationToken cancellationToken)
    {
        var sidecar = await sidecarRepository.GetByIdAsync(request.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), request.SidecarId);

        if (sidecar.Kind == SidecarKind.Whoami && !havenEnvironment.IsDevelopment)
            return Error.Validation("The 'whoami' sidecar is only available in development.");

        sidecar.Enable();
        sidecar.MarkDeploymentPending();
        deploymentJobEnqueuer.EnqueueSidecarDeployment(sidecar.Id);

        return Result.Success();
    }
}