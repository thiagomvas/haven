namespace Haven.Domain;

public enum ServiceStatus
{
    Running,
    Stopped,
    Degraded,
    DeploymentPending,
    Deploying,
    Unknown
}
