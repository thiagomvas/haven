using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Sidecars.Commands.ExportSidecarManifest;

public sealed class ExportSidecarManifestHandler(
    ISidecarRepository sidecarRepository,
    IManifestSerializer<Sidecar> sidecarSerializer)
    : ICommandHandler<ExportSidecarManifestCommand, string>
{
    public async ValueTask<Result<string>> Handle(ExportSidecarManifestCommand request, CancellationToken cancellationToken)
    {
        var sidecar = await sidecarRepository.GetByIdAsync(request.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), request.SidecarId);

        await sidecarSerializer.WriteAsync(sidecar, cancellationToken);

        return await sidecarSerializer.ReadManifestAsync(sidecar, cancellationToken);
    }
}