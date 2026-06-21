using Haven.Application.Common.Messaging;
using Haven.Application.Features.ServiceRegistry;
using Haven.Application.Features.Services.Queries;
using Haven.Domain.Aggregates;
using Haven.Domain.ValueObjects;

using Riok.Mapperly.Abstractions;

using ServiceRegistryEntryDto = Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries.ServiceRegistryEntryDto;

namespace Haven.Application.Mappers;

[Mapper]
public static partial class ServiceRegistryMapper
{
    public static ServiceRegistryEntryDto ToRegistryDto(this ServiceRegistryEntry entry)
    {
        var dto = entry.ToDtoPartial();
        
        if (entry.Service is not null)
        {
            dto.ServiceType = entry.Service.Type;
            dto.ExposureMode = entry.Service.ExposureMode;
        }

        return dto;
    }

    public static PortMappingDto ToDto(this PortMapping entry)
    {
        var dto = entry.ToDtoPartial();
        return dto;
    }

    private static partial ServiceRegistryEntryDto ToDtoPartial(this ServiceRegistryEntry entry);
    [MapProperty(nameof(PortMapping.HostIp), nameof(PortMappingDto.IpAddress))]
    private static partial PortMappingDto ToDtoPartial(this PortMapping entry);
}
