using Haven.Domain;

namespace Haven.Application.Features.Services.Queries;

public sealed record ServiceDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    ServiceType Type,
    ExposureMode ExposureMode,
    ServiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);
