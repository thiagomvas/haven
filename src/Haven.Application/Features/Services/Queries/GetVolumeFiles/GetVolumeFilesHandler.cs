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

        var volumeResult = service.GetManagedVolume(query.VolumeId);
        if (volumeResult.IsFailure)
            return volumeResult.Error;

        return await managedVolumeFileService.ListFilesAsync(query.ServiceId, query.VolumeId, cancellationToken);
    }
}