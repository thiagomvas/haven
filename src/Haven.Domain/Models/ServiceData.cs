using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Models;

/// <summary>
/// Data transfer object for services loaded from manifest files.
/// Used to reconstruct Service domain entities from YAML manifests during synchronization.
/// Serves as an intermediate representation between serialized manifests and domain models.
/// If a service lacks a token when loaded, one will be automatically generated to ensure
/// webhook functionality is never compromised.
/// </summary>
/// <param name="Id">Unique identifier for the service.</param>
/// <param name="EnvironmentId">The environment this service belongs to.</param>
/// <param name="Name">Human-readable name of the service.</param>
/// <param name="Type">Type of service deployment.</param>
/// <param name="ExposureMode">Whether the service is exposed externally or internal only.</param>
/// <param name="Status">Current runtime status of the service.</param>
/// <param name="CreatedAt">Timestamp when the service was created.</param>
/// <param name="UpdatedAt">Timestamp of the last update.</param>
/// <param name="Token">Authentication token for webhook access. Auto-regenerated if missing.</param>
/// <param name="SourceConfig">Source configuration defining how the service is deployed.</param>
public sealed record ServiceData(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    string? Alias,
    ServiceType Type,
    ExposureMode ExposureMode,
    ServiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string Token,
    ServiceSourceConfig? SourceConfig = null);