namespace Haven.Infrastructure.Deployment;

public static class DockerEventTypes
{
    public const string Die = "die";
    public const string Start = "start";
    public const string Stop = "stop";
    public const string Kill = "kill";
    public const string Oom = "oom";

    public static class Health
    {
        public const string Unhealthy = "health_status: unhealthy";
        public const string Healthy = "health_status: healthy";
    }

    public static readonly IReadOnlyCollection<string> CrashLikeEvents = new[]
    {
        Die,
        Oom,
        Health.Unhealthy
    };
}