using Haven.Application.Features.Projects;
using Haven.Domain.Aggregates;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class ProjectMapper
{
    [MapperIgnoreSource(nameof(Project.DomainEvents))]
    [MapperIgnoreSource(nameof(Project.Environments))]
    public static partial ProjectManifestDto ToManifest(this Project project);

    public static Project FromManifest(this ProjectManifestDto dto, IEnumerable<Project.EnvironmentData>? environments = null)
        => Project.Reconstitute(dto.Id, dto.Name, dto.Description, environments);
}