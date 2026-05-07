namespace Haven.Application.Configuration;

public class ManifestsOptions
{
    public const string SectionName = "Manifests";
    public string ManifestsPath { get; set; } = "manifests";
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalSeconds { get; set; } = 60;

    public ManifestsOptions()
    {
        
    }
}
