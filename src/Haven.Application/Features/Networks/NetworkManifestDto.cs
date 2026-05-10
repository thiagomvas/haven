namespace Haven.Application.Features.Networks;

/// <summary>
/// YAML-serializable representation of a Docker network for manifest files.
/// Used to define and version control network configurations for service-to-service communication.
/// Maps to and from Network domain entities for synchronization with the database.
/// </summary>
public sealed record NetworkManifestDto
{
    /// <summary>Human-readable name of the Docker network.</summary>
    public required string Name { get; init; }

    /// <summary>Type of network driver (e.g., "bridge", "overlay"). Used during Docker network creation.</summary>
    public required string Type { get; init; }

    /// <summary>Optional JSON metadata for network configuration customization.</summary>
    public string? Metadata { get; init; }
}
