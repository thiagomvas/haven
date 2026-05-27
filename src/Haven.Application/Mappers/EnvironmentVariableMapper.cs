using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain;
using Haven.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class EnvironmentVariableMapper
{
    public static EnvironmentVariableDto ToDto(this EnvironmentVariables envVar)
        => new()
        {
            Key = envVar.Key,
            Value = envVar.Value ?? string.Empty,
            Scope = envVar.ParentType.ToString()
        };

    public static List<EnvironmentVariableDto> ToDto(this IEnumerable<EnvironmentVariables> envVars)
        => envVars.Select(e => e.ToDto()).ToList();
}
