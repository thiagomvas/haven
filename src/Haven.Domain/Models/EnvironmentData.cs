namespace Haven.Domain.Models;

/// <summary>
/// Data transfer object for environments loaded from manifest files.
/// Used to reconstruct Environment domain entities from YAML manifests during synchronization.
/// Contains the environment's services as ServiceData objects.
/// </summary>
/// <param name="Id">Unique identifier for the environment.</param>
/// <param name="ProjectId">The project this environment belongs to.</param>
/// <param name="Name">Human-readable name (e.g., "dev", "staging", "prod").</param>
/// <param name="Description">Optional description of the environment's purpose.</param>
/// <param name="NetworkName">Docker network name for service-to-service communication.</param>
/// <param name="Services">The services deployed in this environment.</param>
public sealed record EnvironmentData(
    Guid Id,
    Guid ProjectId,
    string Name,
    string? Description,
    string NetworkName,
    IEnumerable<ServiceData>? Services = null);