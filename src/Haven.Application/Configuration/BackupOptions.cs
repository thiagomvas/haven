namespace Haven.Application.Configuration;

public class BackupOptions
{
    public const string SectionName = "Backup";

    public bool Enabled { get; set; } = true;
    public string BackupsPath { get; set; } = "/var/lib/haven/backups";
    public int RetentionCount { get; set; } = 10;
    public string CronExpression { get; set; } = "0 0 * * *";
    public BackupGitOptions Git { get; set; } = new();
}

public class BackupGitOptions
{
    public bool Enabled { get; set; } = false;
    public string? RemoteUrl { get; set; }
    public string Branch { get; set; } = "main";
    public Guid? GitCredentialsId { get; set; }
}
