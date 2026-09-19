using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.HealthChecks;

public class HealthCheckDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? CronExpression { get; set; }
    public string Config { get; set; } = string.Empty;
    public HealthCheckKind Kind { get; set; }
    public DateTime? LastRunAt { get; set; }
    public ServiceHealth LastRunStatus { get; set; }
    public HealthCheckFailureReason LastRunReason { get; set; }
    public string? LastRunMessage { get; set; }
    public long? LastRunDurationMs { get; set; }
    public int Retries { get; set; }
    public int FailureThreshold { get; set; }
    public int SuccessThreshold { get; set; }
    public int ConsecutiveFailures { get; set; }
}
