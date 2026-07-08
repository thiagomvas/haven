using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.Services.Queries.GetVolumeFileContent;

public sealed class GetVolumeFileContentHandler(
    IServiceRepository serviceRepository,
    IManagedVolumeFileService managedVolumeFileService)
    : IQueryHandler<GetVolumeFileContentQuery, string>
{
    public async ValueTask<Result<string>> Handle(GetVolumeFileContentQuery query, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(query.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), query.ServiceId);

        var volumeResult = service.GetManagedVolume(query.VolumeId);
        if (volumeResult.IsFailure)
            return volumeResult.Error;

        return await managedVolumeFileService.ReadFileAsync(query.ServiceId, query.VolumeId, query.Path, cancellationToken);
    }
}
