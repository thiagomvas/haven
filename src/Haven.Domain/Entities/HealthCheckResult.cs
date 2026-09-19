using Haven.Domain.Enums;
using Haven.Domain.Models;

namespace Haven.Domain.Entities;

/// <summary>
/// A persisted record of one health check run. Only the most recent runs per check are retained.
/// </summary>
public class HealthCheckResult : Entity
{
    public Guid HealthCheckId { get; private set; }
    public DateTime RanAt { get; private set; }
    public ServiceHealth Status { get; private set; }
    public HealthCheckFailureReason Reason { get; private set; }
    public string? Message { get; private set; }
    public long DurationMs { get; private set; }
    public int? HttpStatusCode { get; private set; }
    public long? ExitCode { get; private set; }
    public string? Output { get; private set; }
    public int Attempts { get; private set; }

    private HealthCheckResult()
    {
    }

    public static HealthCheckResult Create(Guid healthCheckId, HealthCheckRunResult result, DateTime ranAt) =>
        new()
        {
            HealthCheckId = healthCheckId,
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
