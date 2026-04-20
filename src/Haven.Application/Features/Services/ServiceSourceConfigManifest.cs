using Haven.Domain;

namespace Haven.Application.Features.Services;

public sealed class ServiceSourceConfigManifest
{
    public string Type { get; set; } = string.Empty;
    public string? Image { get; set; }
    public List<string> Ports { get; set; } = [];
    public List<string> Volumes { get; set; } = [];
    public List<string> EnvironmentVariables { get; set; } = [];
    public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.UnlessStopped;
}
