using Haven.Application.Features.FeatureFlags;
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
    public required Guid Id { get; set; }

    /// <summary>The environment this service belongs to.</summary>
    public required Guid EnvironmentId { get; set; }

    /// <summary>Human-readable name of the service (e.g., "api", "db", "cache").</summary>
    public required string Name { get; set; }

    /// <summary>Short alias for Docker resource naming (2–8 chars, lowercase alphanumeric/hyphens).</summary>
    public string? Alias { get; set; }

    /// <summary>Type of service (e.g., DockerImage, External).</summary>
    public required ServiceType Type { get; set; }

    /// <summary>Whether the service is exposed externally or only internal to the environment.</summary>
    public required ExposureMode ExposureMode { get; set; }

    /// <summary>Current runtime status of the service (Running, Stopped, DeploymentPending, etc.).</summary>
    public required ServiceStatus Status { get; set; }

    /// <summary>Source configuration defining how the service is deployed (e.g., Docker image details).</summary>
    public ServiceSourceConfigManifest? SourceConfig { get; set; }

    /// <summary>Timestamp when the service was created.</summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>Timestamp of the last update to the service.</summary>
    public required DateTime UpdatedAt { get; set; }
    
    public ICollection<FeatureFlagManifest> FeatureFlags { get; set; } = new List<FeatureFlagManifest>();

    /// <summary>
    /// Authentication token for webhook access (e.g., deployment triggers).
    /// Auto-regenerated if missing during manifest synchronization.
    /// </summary>
    public required string Token { get; set; }
}
