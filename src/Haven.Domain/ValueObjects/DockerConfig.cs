namespace Haven.Domain.ValueObjects;

public sealed class DockerConfig : ServiceSourceConfig
{
    public string Image { get; set; } = string.Empty;
    public List<string> Ports { get; set; } = [];
    public List<string> Volumes { get; set; } = [];
    public List<string> EnvironmentVariables { get; set; } = [];
    public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.UnlessStopped;
}
