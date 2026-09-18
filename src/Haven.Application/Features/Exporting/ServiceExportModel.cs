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
    /// The path to the Dockerfile that the service uses.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/> when <see cref="Image"/> is specified.
    /// </remarks>
    public string? DockerfilePath { get; set; }

    /// <summary>
    /// The resolved environment variables the service runs with, i.e. after merging project, environment and
    /// service-level overrides (service overrides win) and appending feature flags as environment variables.
    /// </summary>
    public Dictionary<string, string?> EnvironmentVariables { get; set; } = [];

    /// <summary>
    /// The volumes mounted into the service's container.
    /// </summary>
    public List<ServiceExportVolumeModel> Volumes { get; set; } = [];
}