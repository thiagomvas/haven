using Haven.Application.Features.Networks;
using Haven.Application.Features.Networks.Queries.ListNetworks;
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

    public static NetworkDto ToDto(this Network network)
    {
        var services = network.ServiceNetworks
            .Where(sn => sn.Service is not null)
            .Select(sn => new NetworkServiceDto
            {
                Id = sn.Service!.Id,
                Name = sn.Service.Name,
                Status = sn.Service.Status.ToString()
            })
            .ToList();

        return new NetworkDto
        {
            Id = network.Id,
            Name = network.Name,
            Type = network.Type.ToString(),
            ProjectId = network.ProjectId,
            ProjectName = network.Project?.Name,
            EnvironmentId = network.EnvironmentId,
            EnvironmentName = network.Environment?.Name,
            ServiceCount = services.Count,
            Services = services,
            CreatedAt = network.CreatedAt
        };
    }
}