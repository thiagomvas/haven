namespace Haven.Application.Configuration;

public class ManifestsOptions
{
    public const string SectionName = "Manifests";
    public string ManifestsPath { get; set; } = "/data/manifests";
    public bool IncludeEnvValuesOnExample { get; set; } = true;
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalSeconds { get; set; } = 60;

    public ManifestsOptions()
    {

    }
}