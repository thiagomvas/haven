using Haven.Application.Features.HealthChecks;
using Haven.Domain.Entities;
using Haven.Domain.Models;

using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class HealthCheckMapper
{
    public static partial HealthCheckDto ToDto(this HealthCheck healthCheck);

    public static partial HealthCheckResultDto ToDto(this HealthCheckResult result);

    public static IReadOnlyList<HealthCheckDto> ToDtos(this IEnumerable<HealthCheck> healthChecks) =>
        healthChecks.Select(ToDto).ToList();

    public static IReadOnlyList<HealthCheckResultDto> ToDtos(this IEnumerable<HealthCheckResult> results) =>
        results.Select(ToDto).ToList();

    /// <summary>Maps a run result that was not persisted (Run now / Test), stamping it with the given time.</summary>
    public static HealthCheckResultDto ToDto(this HealthCheckRunResult result, DateTime ranAt) =>
        new()
        {
            RanAt = ranAt,
            Status = result.Status,
            Reason = result.Reason,
            Message = result.Message,
            DurationMs = result.DurationMs,
            HttpStatusCode = result.HttpStatusCode,
            ExitCode = result.ExitCode,
            Output = result.Output,
            Attempts = result.Attempts
        };
}
