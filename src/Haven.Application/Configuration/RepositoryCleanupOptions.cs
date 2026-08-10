namespace Haven.Application.Configuration;

public class RepositoryCleanupOptions
{
    public const string SectionName = "RepositoryCleanup";

    public bool Enabled { get; set; } = true;
    public string CronExpression { get; set; } = "0 4 * * *";
    public int GracePeriodHours { get; set; } = 24;
    public bool DryRun { get; set; } = false;
}