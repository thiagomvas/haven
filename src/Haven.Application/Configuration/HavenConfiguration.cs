namespace Haven.Application.Configuration;

public sealed class HavenConfiguration
{
    public ManifestsOptions Manifests { get; set; } = new();
    public InstanceOptions Instance { get; set; } = new();
    public NetworkOptions Network { get; set; } = new();
    public BackupOptions Backup { get; set; } = new();
}