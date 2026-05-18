using Haven.Application.Features.Projects;
using Haven.Application.Features.Projects.Queries.GetProjects;
using Haven.Domain.Aggregates;
using Haven.Domain.Models;
using Riok.Mapperly.Abstractions;


namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class ProjectMapper
{
    [MapperIgnoreSource(nameof(Project.DomainEvents))]
    [MapperIgnoreSource(nameof(Project.Environments))]
    public static partial ProjectManifestDto ToManifest(this Project project);

    public static Project FromManifest(this ProjectManifestDto dto, IEnumerable<EnvironmentData>? environments = null)
        => Project.Reconstitute(dto.Id, dto.Name, dto.Description, environments);

    private static partial ProjectDto ToDtoPartial(this Project project);

    public static ProjectDto ToDto(this Project project)
    {
        return project.ToDtoPartial();
    }
}