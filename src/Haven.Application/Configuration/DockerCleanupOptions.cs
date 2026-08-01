namespace Haven.Application.Configuration;

public class DockerCleanupOptions
{
    public const string SectionName = "DockerCleanup";

    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 3 * * *";
    public int GracePeriodHours { get; set; } = 24;
    public bool DryRun { get; set; } = false;
}
