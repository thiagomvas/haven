using System.Text.Json.Serialization;

using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.Models;

namespace Haven.Domain.Entities;

public class HealthCheck : Entity
{
    public string Name { get; set; }
    public bool Enabled { get; set; }
    public string? CronExpression { get; set; }
    public string Config { get; set; } = string.Empty;
    public HealthCheckKind Kind { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime? LastRunAt { get; set; }
    public ServiceHealth LastRunStatus { get; set; }
    public HealthCheckFailureReason LastRunReason { get; set; }
    public string? LastRunMessage { get; set; }
    public long? LastRunDurationMs { get; set; }

    /// <summary>Extra attempts made within one run before a failure is reported.</summary>
    public int Retries { get; set; }

    /// <summary>Consecutive failed runs required before the check is considered unhealthy.</summary>
    public int FailureThreshold { get; set; } = 1;

    /// <summary>Consecutive successful runs required before an unhealthy check is considered healthy again.</summary>
    public int SuccessThreshold { get; set; } = 1;

    public int ConsecutiveFailures { get; private set; }
    public int ConsecutiveSuccesses { get; private set; }

    [JsonIgnore]
    public Service? Service { get; set; }

    private HealthCheck()
    {
    }

    public static HealthCheck Create(
        Guid serviceId,
        string name,
        HealthCheckKind kind,
        bool enabled,
        string? cronExpression,
        string config,
        int retries = 0,
        int failureThreshold = 1,
        int successThreshold = 1) =>
        new()
        {
            ServiceId = serviceId,
            Name = name,
            Kind = kind,
            Enabled = enabled,
            CronExpression = cronExpression,
            Config = config,
            Retries = retries,
            FailureThreshold = failureThreshold,
            SuccessThreshold = successThreshold,
            LastRunStatus = ServiceHealth.Unknown
        };

    /// <summary>
    /// Records a run and updates <see cref="LastRunStatus"/> taking the failure/success thresholds into account.
    /// </summary>
    public void ApplyResult(HealthCheckRunResult result, DateTime ranAt)
    {
        LastRunAt = ranAt;
        LastRunReason = result.Reason;
        LastRunMessage = result.Message;
        LastRunDurationMs = result.DurationMs;

        switch (result.Status)
        {
            case ServiceHealth.Healthy:
                ConsecutiveSuccesses++;
                ConsecutiveFailures = 0;
                if (LastRunStatus != ServiceHealth.Unhealthy || ConsecutiveSuccesses >= Math.Max(1, SuccessThreshold))
                    LastRunStatus = ServiceHealth.Healthy;
                break;

            case ServiceHealth.Unhealthy:
                ConsecutiveFailures++;
                ConsecutiveSuccesses = 0;
                if (ConsecutiveFailures >= Math.Max(1, FailureThreshold))
                    LastRunStatus = ServiceHealth.Unhealthy;
                break;

            default:
                ConsecutiveFailures = 0;
                ConsecutiveSuccesses = 0;
                LastRunStatus = ServiceHealth.Unknown;
                break;
        }
    }
}
