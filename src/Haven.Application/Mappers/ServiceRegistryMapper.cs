using Haven.Application.Features.Services.Queries;
using Haven.Domain.Aggregates;

namespace Haven.Application.Mappers;

public static class ServiceRegistryMapper
{
    public static ServiceRegistryEntryDto ToRegistryDto(this ServiceRegistryEntry entry) =>
        new()
        {
            ContainerName = entry.ContainerName,
            IpAddress = entry.IpAddress,
            Ports = entry.Ports.Select(p => new PortMappingDto
            {
                HostPort = p.HostPort,
                ContainerPort = p.ContainerPort
            }).ToList(),
            Status = entry.Status,
            RegisteredAt = entry.RegisteredAt,
            UpdatedAt = entry.UpdatedAt
        };
}
