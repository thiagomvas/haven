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

        var volume = service.Volumes.FirstOrDefault(v => v.Id == query.VolumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), query.VolumeId);

        if (volume.Type != VolumeType.Managed)
            return Error.InvalidOperation("File operations are only supported for managed volumes.");

        return await managedVolumeFileService.ReadFileAsync(query.ServiceId, query.VolumeId, query.Path, cancellationToken);
    }
}
