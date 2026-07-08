using Haven.Application.Features.Services;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ServiceVolumeMapper
{
    public static partial ServiceVolumeDto ToDto(this ServiceVolume volume);

    public static partial IReadOnlyList<ServiceVolumeDto> ToDtos(this IEnumerable<ServiceVolume> volumes);

    public static partial VolumeManifest ToManifest(this ServiceVolume volume);

    public static ServiceVolume ToEntity(this VolumeManifest manifest, Guid serviceId)
    {
        var now = DateTime.UtcNow;
        return ServiceVolume.Reconstitute(
            manifest.Id == Guid.Empty ? Guid.CreateVersion7() : manifest.Id,
            serviceId,
            manifest.Type,
            manifest.Name,
            manifest.Target,
            manifest.Source,
            manifest.ReadOnly,
            manifest.BackupEnabled,
            now,
            now);
    }
}