namespace Haven.Application.Features.Exporting.Exporters.DockerCompose;

public class ComposeFile
{
    public Dictionary<string, ComposeService> Services { get; set; } = [];

    /// <summary>
    /// Top-level declarations for named volumes referenced by <see cref="Services"/>, as required by the
    /// Compose spec. Bind mounts (host paths and Haven-managed volumes) do not need a declaration here.
    /// </summary>
    public Dictionary<string, object?> Volumes { get; set; } = [];

    /// <summary>
    /// Top-level declarations for the additional networks referenced by <see cref="Services"/>. Services
    /// that reference one also join <c>default</c>, which stands in for the environment network.
    /// </summary>
    public Dictionary<string, object?> Networks { get; set; } = [];
}
