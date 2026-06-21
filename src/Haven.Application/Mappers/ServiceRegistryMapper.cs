using Haven.Application.Features.Services.Queries;
using Haven.Domain.Aggregates;
using PagedEntryDto = Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries.ServiceRegistryEntryDto;

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

    public static PagedEntryDto ToPagedDto(this ServiceRegistryEntry entry) =>
        new(
            entry.Id,
            entry.ServiceId,
            entry.ContainerName,
            entry.IpAddress,
            entry.Ports,
            entry.Status.ToString(),
            entry.RegisteredAt,
            entry.UpdatedAt);
}
