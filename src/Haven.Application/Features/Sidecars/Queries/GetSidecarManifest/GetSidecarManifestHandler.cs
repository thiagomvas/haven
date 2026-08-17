using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Sidecars.Queries.GetSidecarManifest;

public sealed class GetSidecarManifestHandler(
    ISidecarRepository repository,
    IManifestSerializer<Sidecar> serializer)
    : IQueryHandler<GetSidecarManifestQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetSidecarManifestQuery query, CancellationToken cancellationToken)
    {
        var sidecar = await repository.GetByIdAsync(query.SidecarId, cancellationToken);
        if (sidecar is null)
            return Error.NotFoundFor(nameof(Sidecar), query.SidecarId);

        try
        {
            return await serializer.ReadManifestAsync(sidecar, cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return Error.Validation($"No manifest file exists on disk for sidecar '{sidecar.Name}' yet. Export it first.");
        }
    }
}
