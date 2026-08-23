namespace Haven.Application.Configuration;

public class ManifestsOptions
{
    public const string SectionName = "Manifests";
    public string ManifestsPath { get; set; } = "/data/manifests";
    public bool IncludeEnvValuesOnExample { get; set; } = true;

    /// <summary>
    /// How long <see cref="Haven.Infrastructure.Backup.ManifestSyncBackgroundService"/> waits for
    /// mutations to go quiet before writing a full manifest resync, so a burst of changes (bulk
    /// import, a restore's own writes) collapses into a single write instead of one per mutation.
    /// </summary>
    public int SyncDebounceSeconds { get; set; } = 3;

    public ManifestsOptions()
    {

    }
}