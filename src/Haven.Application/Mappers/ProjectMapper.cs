using Haven.Application.Dtos;
using Haven.Domain.Aggregates;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class ProjectMapper
{
    [MapperIgnoreSource(nameof(Project.DomainEvents))]
    public static partial ProjectManifestDto ToManifest(this Project project);

    public static Project FromManifest(this ProjectManifestDto dto)
        => Project.Reconstitute(dto.Id, dto.Name, dto.Description);
}