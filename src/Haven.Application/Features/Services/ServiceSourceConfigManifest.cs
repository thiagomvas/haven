using Haven.Domain;

namespace Haven.Application.Features.Services;

public sealed class ServiceSourceConfigManifest
{
    public string Type { get; set; } = string.Empty;

    // DockerConfig fields
    public string? Image { get; set; }
    public List<string> Ports { get; set; } = [];
    public List<string> Volumes { get; set; } = [];
    public List<string> EnvironmentVariables { get; set; } = [];
    public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.UnlessStopped;

    // DockerfileConfig fields
    public DockerfileSource? DockerfileSource { get; set; }
    public string? Repository { get; set; }
    public string? Branch { get; set; }
    public string? FilePath { get; set; }
    public Guid? GitCredentialId { get; set; }
    public string? Content { get; set; }
}
