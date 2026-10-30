namespace Haven.Application.Features.Exporting;

/// <summary>
/// Intermediate representation of a service that'll be exported to a different format (e.g. Docker Compose).
/// </summary>
public class ServiceExportModel
{
    /// <summary>
    /// The name of the service that it'll be exported as.
    /// </summary>
    /// <remarks>
    /// Should be set as <see cref="Domain.Aggregates.Service.Alias"/>
    /// </remarks>
    public string Name { get; set; }

    /// <summary>
    /// The image that the service uses.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when <see cref="DockerfilePath"/> is specified.
    /// </remarks>
    public string? Image { get; set; }

    /// <summary>
    /// The path to the Dockerfile that the service uses, relative to <see cref="BuildContext"/>.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when <see cref="Image"/> or <see cref="DockerfileContent"/> is specified.
    /// </remarks>
    public string? DockerfilePath { get; set; }

    /// <summary>
    /// The raw Dockerfile content for services whose Dockerfile is authored in Haven rather than a repository.
    /// </summary>
    public string? DockerfileContent { get; set; }

    /// <summary>
    /// The build context: a git URL (optionally with <c>#branch:subdir</c>) or a path inside the repository.
    /// </summary>
    public string? BuildContext { get; set; }

    /// <summary>
    /// Build arguments passed to the image build.
    /// </summary>
    public Dictionary<string, string> BuildArgs { get; set; } = [];

    /// <summary>
    /// Port mappings in Compose syntax, already adjusted for the service's exposure mode.
    /// </summary>
    public List<string> Ports { get; set; } = [];

    /// <summary>
    /// Arguments passed to the container as its command.
    /// </summary>
    public List<string> Command { get; set; } = [];

    /// <summary>
    /// The restart policy in Compose syntax (<c>no</c>, <c>always</c>, <c>unless-stopped</c>, <c>on-failure</c>).
    /// </summary>
    public string? Restart { get; set; }

    /// <summary>Memory limit in bytes, or <see langword="null"/> for no limit.</summary>
    public long? MemoryLimit { get; set; }

    /// <summary>Memory plus swap limit in bytes (<c>-1</c> for unlimited swap), or <see langword="null"/> for Docker's default.</summary>
    public long? MemorySwap { get; set; }

    /// <summary>Soft memory reservation in bytes, or <see langword="null"/> for none.</summary>
    public long? MemoryReservation { get; set; }

    /// <summary>CPU limit in (fractional) cores, or <see langword="null"/> for no limit.</summary>
    public decimal? Cpus { get; set; }

    /// <summary>
    /// Names of the additional (non environment-default) networks the service is attached to.
    /// </summary>
    public List<string> Networks { get; set; } = [];

    /// <summary>
    /// The resolved environment variables the service runs with, i.e. after merging project, environment and
    /// service-level overrides (service overrides win) and appending feature flags as environment variables.
    /// Secret values are never included here, see <see cref="SecretKeys"/>.
    /// </summary>
    public Dictionary<string, string?> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// Keys of the service's secret variables. Exported as <c>${KEY}</c> references so the secret values
    /// never end up in the generated file.
    /// </summary>
    public List<string> SecretKeys { get; set; } = [];

    /// <summary>
    /// The volumes mounted into the service's container.
    /// </summary>
    public List<ServiceExportVolumeModel> Volumes { get; set; } = [];
}