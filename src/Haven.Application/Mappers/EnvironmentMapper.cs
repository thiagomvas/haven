using Haven.Application.Features.Environments;
using Haven.Domain.Aggregates;
using Haven.Domain.Models;
using Riok.Mapperly.Abstractions;
using Environment = Haven.Domain.Entities.Environment;


namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentMapper
{
    [MapperIgnoreSource(nameof(Environment.Services))]
    public static partial EnvironmentManifestDto ToManifest(this Environment environment);

    public static EnvironmentData ToEnvironmentData(this EnvironmentManifestDto dto, IEnumerable<ServiceData>? services = null)
        => new(dto.Id, dto.ProjectId, dto.Name, dto.Description, dto.NetworkName, services);
}
