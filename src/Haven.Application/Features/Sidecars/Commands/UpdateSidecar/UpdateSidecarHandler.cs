using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Sidecars.Commands.UpdateSidecar;

public sealed class UpdateSidecarHandler(
    ISidecarRepository sidecarRepository,
    IDeploymentJobEnqueuer deploymentJobEnqueuer) : ICommandHandler<UpdateSidecarCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateSidecarCommand request, CancellationToken cancellationToken)
    {
        var sidecar = await sidecarRepository.GetByIdAsync(request.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), request.SidecarId);

        Optional<ServiceSourceConfig?> sourceConfig = request.DockerConfig.HasValue
            ? (Optional<ServiceSourceConfig?>)request.DockerConfig.Value
            : default;

        var hasChanges = sidecar.Update(default, default, sourceConfig);

        if (hasChanges && sidecar.Enabled)
        {
            sidecar.MarkDeploymentPending();
            deploymentJobEnqueuer.EnqueueSidecarDeployment(sidecar.Id);
        }

        return Result<Guid>.Success(sidecar.Id);
    }
}
