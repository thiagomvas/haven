using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Queries.GetVolumeFiles;

public sealed class GetVolumeFilesHandler(
    IServiceRepository serviceRepository,
    IManagedVolumeFileService managedVolumeFileService)
    : IQueryHandler<GetVolumeFilesQuery, IReadOnlyList<ManagedVolumeFileEntry>>
{
    public async ValueTask<Result<IReadOnlyList<ManagedVolumeFileEntry>>> Handle(GetVolumeFilesQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), query.ServiceId);

        var volume = service.Volumes.FirstOrDefault(v => v.Id == query.VolumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), query.VolumeId);

        if (volume.Type != VolumeType.Managed)
            return Error.InvalidOperation("File operations are only supported for managed volumes.");

        return await managedVolumeFileService.ListFilesAsync(query.ServiceId, query.VolumeId, cancellationToken);
    }
}
