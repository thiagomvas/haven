using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services.Queries;

public sealed record ServiceDto(
    Guid Id,
    Guid EnvironmentId,
    string Name,
    ServiceType Type,
    ExposureMode ExposureMode,
    ServiceStatus Status,
    ServiceSourceConfig? SourceConfig,
    DateTime CreatedAt,
    DateTime UpdatedAt);
