using Haven.Domain;

namespace Haven.Application.Features.Services;

public sealed class ServiceManifestDto
{
    public required Guid Id { get; init; }
    public required Guid EnvironmentId { get; init; }
    public required string Name { get; init; }
    public required ServiceType Type { get; init; }
    public required ExposureMode ExposureMode { get; init; }
    public required ServiceStatus Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
