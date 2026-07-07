using Haven.Application.Features.Services;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ServiceVolumeMapper
{
    public static partial ServiceVolumeDto ToDto(this ServiceVolume volume);

    public static partial IReadOnlyList<ServiceVolumeDto> ToDtos(this IEnumerable<ServiceVolume> volumes);
}
