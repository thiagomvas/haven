namespace Haven.Domain;

public enum HealthStatus
{
    Running,
    Healthy,
    Degraded,
    Stopped,
    Died,
    Unknown
}