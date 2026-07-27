using System.Text.Json.Serialization;

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
    
    [JsonIgnore]
    public Service? Service { get; set; }

    private HealthCheck()
    {
    }

    public static HealthCheck Create(Guid serviceId, string name, HealthCheckKind kind, bool enabled, string? cronExpression, string config) =>
        new()
        {
            ServiceId = serviceId,
            Name = name,
            Kind = kind,
            Enabled = enabled,
            CronExpression = cronExpression,
            Config = config,
            LastRunStatus = ServiceHealth.Unknown
        };
}