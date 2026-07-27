using Haven.Application.Features.HealthChecks;
using Haven.Domain.Entities;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class HealthCheckMapper
{
    public static partial HealthCheckDto ToDto(this HealthCheck healthCheck);

    public static IReadOnlyList<HealthCheckDto> ToDtos(this IEnumerable<HealthCheck> healthChecks) =>
        healthChecks.Select(ToDto).ToList();
}
