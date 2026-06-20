namespace Haven.Application.Configuration;

public class InstanceOptions
{
    public const string SectionName = "instance";
    public string InstanceName { get; set; } = string.Empty;
    public string Timezone { get; set; } = "UTC";
    public TimeFormat TimeFormat { get; set; } = TimeFormat.Hour12;
    public int DeploymentLogRetentionCount { get; set; } = 10;
}