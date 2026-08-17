using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Mappers;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Sidecars.Commands.ImportSidecarManifest;

public sealed class ImportSidecarManifestHandler(
    ISidecarRepository sidecarRepository,
    IManifestSerializer<Sidecar> sidecarSerializer,
    IManifestParser<SidecarManifestDto> sidecarManifestParser)
    : ICommandHandler<ImportSidecarManifestCommand>
{
    public async ValueTask<Result> Handle(ImportSidecarManifestCommand request, CancellationToken cancellationToken)
    {
        var sidecar = await sidecarRepository.GetByIdAsync(request.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), request.SidecarId);

        string yaml;
        if (!string.IsNullOrWhiteSpace(request.ManifestYaml))
        {
            yaml = request.ManifestYaml;
        }
        else
        {
            try
            {
                yaml = await sidecarSerializer.ReadManifestAsync(sidecar, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                return Error.NotFoundFor("Sidecar manifest file", request.SidecarId);
            }
        }

        var manifest = await sidecarManifestParser.ParseAsync(yaml, cancellationToken);

        sidecar.Update(manifest.Name, manifest.Alias, manifest.SourceConfig.ToDomain());

        return Result.Success();
    }
}
