namespace Haven.Application.Features.Networks;

/// <summary>
/// YAML-serializable representation of a Docker network for manifest files.
/// Used to define and version control network configurations for service-to-service communication.
/// Maps to and from Network domain entities for synchronization with the database.
/// </summary>
public sealed record NetworkManifestDto
{
    /// <summary>Unique identifier for the network, used to preserve identity across syncs.</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable name of the Docker network.</summary>
    public required string Name { get; init; }

    /// <summary>Type of network driver (e.g., "bridge", "overlay"). Used during Docker network creation.</summary>
    public required string Type { get; init; }

    /// <summary>Optional JSON metadata for network configuration customization.</summary>
    public string? Metadata { get; init; }

    /// <summary>Docker-assigned IPAM subnet (CIDR notation), if known.</summary>
    public string? Subnet { get; init; }

    /// <summary>Docker-assigned IPAM gateway address, if known.</summary>
    public string? Gateway { get; init; }
}