namespace Haven.Domain.Enums;

public enum HealthStatus
{
    Running,
    Healthy,
    Degraded,
    Stopped,
    Died,
    Unknown,
    Deploying,
    DeploymentPending
}