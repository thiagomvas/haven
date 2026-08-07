using Haven.Application.Common;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Services;

public static class ServiceVolumeExtensions
{
    /// <summary>
    /// Finds a volume on the service by id and verifies it is a <see cref="VolumeType.Managed"/>
    /// volume, since only managed volumes support file operations.
    /// </summary>
    public static Result<ServiceVolume> GetManagedVolume(this Service service, Guid volumeId)
    {
        var volume = service.Volumes.FirstOrDefault(v => v.Id == volumeId);
        if (volume is null)
            return Error.NotFoundFor(nameof(ServiceVolume), volumeId);

        if (volume.Type != VolumeType.Managed)
            return Error.InvalidOperation("File operations are only supported for managed volumes.");

        return volume;
    }
}