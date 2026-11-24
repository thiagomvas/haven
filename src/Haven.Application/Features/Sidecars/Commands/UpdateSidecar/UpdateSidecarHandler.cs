using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services;
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

        ServiceSourceConfig? incomingConfig = request.DockerConfig.HasValue ? request.DockerConfig.Value : null;
        var hasConfigUpdate = request.DockerConfig.HasValue;

        // Limits sent on their own are applied to a copy of the stored config (the getter deserializes a fresh one).
        if (!hasConfigUpdate && request.ResourceLimits.HasValue && sidecar.SourceConfig is DockerBasedSourceConfig stored)
        {
            (request.ResourceLimits.Value ?? new ServiceResourceLimits()).ApplyTo(stored);
            incomingConfig = stored;
            hasConfigUpdate = true;
        }
        else if (incomingConfig is DockerBasedSourceConfig incomingDocker)
        {
            // The sidecar UI edits image/ports/args only, so a replacement config carries no limits of its own.
            var limits = request.ResourceLimits.HasValue
                ? request.ResourceLimits.Value
                : sidecar.SourceConfig is DockerBasedSourceConfig existing
                    ? new ServiceResourceLimits
                    {
                        MemoryLimit = existing.MemoryLimit,
                        MemorySwap = existing.MemorySwap,
                        MemoryReservation = existing.MemoryReservation,
                        NanoCpus = existing.NanoCpus
                    }
                    : null;
            (limits ?? new ServiceResourceLimits()).ApplyTo(incomingDocker);
        }

        Optional<ServiceSourceConfig?> sourceConfig = hasConfigUpdate
            ? (Optional<ServiceSourceConfig?>)incomingConfig
            : default;

        var hasChanges = sidecar.Update(default, default, sourceConfig);

        if (hasChanges && sidecar.Enabled)
        {
            await deploymentJobEnqueuer.EnqueueSidecarDeployment(sidecar.Id, cancellationToken);
        }

        return Result<Guid>.Success(sidecar.Id);
    }
}