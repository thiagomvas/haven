namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeFile
{
    public Dictionary<string, ComposeService> Services { get; set; } = [];

    /// <summary>
    /// Top-level declarations for named volumes referenced by <see cref="Services"/>, as required by the
    /// Compose spec. Bind mounts (host paths and Haven-managed volumes) do not need a declaration here.
    /// </summary>
    public Dictionary<string, object?> Volumes { get; set; } = [];
}