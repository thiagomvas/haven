using Haven.Application.Features.Networks;
using Haven.Domain.Aggregates;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class NetworkMapper
{
    [MapperIgnoreSource(nameof(Network.ServiceNetworks))]
    [MapperIgnoreSource(nameof(Network.Project))]
    [MapperIgnoreSource(nameof(Network.Environment))]
    public static partial NetworkManifestDto ToManifest(this Network network);

    public static Network FromManifest(this NetworkManifestDto dto, Guid projectId, Guid environmentId)
    {
        var type = Enum.Parse<Haven.Domain.NetworkType>(dto.Type);
        return Network.Reconstitute(
            dto.Id,
            dto.Name,
            type,
            dto.Metadata,
            projectId,
            environmentId,
            DateTime.UtcNow,
            DateTime.UtcNow);
    }
}
