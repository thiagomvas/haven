using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

public sealed record ServiceRegistryEntryDto(
    Guid Id,
    Guid ServiceId,
    string? ContainerName,
    string? IpAddress,
    List<PortMapping> Ports,
    string Status,
    DateTime RegisteredAt,
    DateTime UpdatedAt);
