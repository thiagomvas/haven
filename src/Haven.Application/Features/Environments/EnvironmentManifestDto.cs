namespace Haven.Application.Features.Environments;

/// <summary>
/// YAML-serializable representation of an environment for manifest files.
/// Used to persist and version control environment definitions alongside infrastructure code.
/// Maps to and from Environment domain entities for synchronization with the database.
/// </summary>
public sealed class EnvironmentManifestDto
{
    /// <summary>Unique identifier for the environment.</summary>
    public required Guid Id { get; init; }

    /// <summary>The project this environment belongs to.</summary>
    public required Guid ProjectId { get; init; }

    /// <summary>Human-readable name of the environment (e.g., "dev", "staging", "prod").</summary>
    public required string Name { get; init; }

    /// <summary>Optional description of the environment's purpose or constraints.</summary>
    public required string? Description { get; init; }

    /// <summary>Docker network name used for service-to-service communication in this environment.</summary>
    public required string NetworkName { get; init; }
}
