using Haven.Application.Features.Services;

namespace Haven.Application.Features.Sidecars;

/// <summary>
/// YAML-serializable representation of a sidecar for manifest files.
/// Maps to and from Sidecar domain entities for synchronization with the database.
/// </summary>
public sealed record SidecarManifestDto
{
    /// <summary>Unique identifier for the sidecar, used to preserve identity across syncs.</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable name of the sidecar.</summary>
    public required string Name { get; init; }

    /// <summary>Short alias for Docker resource naming.</summary>
    public string? Alias { get; init; }

    /// <summary>Kind of built-in sidecar (e.g., "Traefik", "Whoami", "Custom").</summary>
    public required string Kind { get; init; }

    /// <summary>Source configuration defining how the sidecar is deployed, for sidecars of kind Custom.</summary>
    public ServiceSourceConfigManifest? SourceConfig { get; init; }
}
