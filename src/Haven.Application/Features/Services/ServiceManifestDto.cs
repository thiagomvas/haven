using Haven.Domain;

namespace Haven.Application.Features.Services;

/// <summary>
/// YAML-serializable representation of a service for manifest files.
/// Used to persist and version control service definitions alongside infrastructure code.
/// Maps to and from Service domain entities for synchronization with the database.
/// </summary>
public sealed class ServiceManifestDto
{
    /// <summary>Unique identifier for the service.</summary>
    public required Guid Id { get; init; }

    /// <summary>The environment this service belongs to.</summary>
    public required Guid EnvironmentId { get; init; }

    /// <summary>Human-readable name of the service (e.g., "api", "db", "cache").</summary>
    public required string Name { get; init; }

    /// <summary>Type of service (e.g., DockerImage, External).</summary>
    public required ServiceType Type { get; init; }

    /// <summary>Whether the service is exposed externally or only internal to the environment.</summary>
    public required ExposureMode ExposureMode { get; init; }

    /// <summary>Current runtime status of the service (Running, Stopped, DeploymentPending, etc.).</summary>
    public required ServiceStatus Status { get; init; }

    /// <summary>Source configuration defining how the service is deployed (e.g., Docker image details).</summary>
    public ServiceSourceConfigManifest? SourceConfig { get; init; }

    /// <summary>Timestamp when the service was created.</summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>Timestamp of the last update to the service.</summary>
    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Authentication token for webhook access (e.g., deployment triggers).
    /// Auto-regenerated if missing during manifest synchronization.
    /// </summary>
    public required string Token { get; init; }
}
