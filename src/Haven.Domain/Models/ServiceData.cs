using Haven.Domain.ValueObjects;

namespace Haven.Domain.Models;

public sealed record ServiceData(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    ServiceType Type,
    ExposureMode ExposureMode,
    ServiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    ServiceSourceConfig? SourceConfig = null);