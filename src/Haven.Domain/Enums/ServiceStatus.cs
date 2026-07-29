namespace Haven.Domain;

public enum ServiceStatus
{
    Running,
    Stopped,
    DeploymentPending,
    Deploying,
    Unknown
}