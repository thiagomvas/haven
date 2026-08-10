using Haven.Domain.Enums;

namespace Haven.Domain.ValueObjects;

public sealed class DockerfileConfig : ServiceSourceConfig
{
    public DockerfileSource Source { get; set; }

    public string? Repository { get; set; }
    public string? Branch { get; set; }
    public string? FilePath { get; set; }
    public Guid? GitCredentialId { get; set; }

    public string? Content { get; set; }

    public RestartPolicy RestartPolicy { get; set; } = RestartPolicy.UnlessStopped;
}