using Haven.Application.Features.Environments;
using Haven.Domain.Aggregates;
using Riok.Mapperly.Abstractions;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentMapper
{
    [MapperIgnoreSource(nameof(Environment.Services))]
    public static partial EnvironmentManifestDto ToManifest(this Environment environment);

    public static Project.EnvironmentData ToEnvironmentData(this EnvironmentManifestDto dto, IEnumerable<Project.ServiceData>? services = null)
        => new(dto.Id, dto.ProjectId, dto.Name, dto.Description, dto.NetworkName, services);
}
