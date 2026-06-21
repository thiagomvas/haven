using Haven.Application.Features.Services.Queries;
using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceRegistry.Queries.GetServiceRegistryEntries;

public sealed class ServiceRegistryEntryDto
{
    public string? ContainerName { get; set; }
    public string? IpAddress { get; set; }
    public List<PortMappingDto> Ports { get; set; } = [];
    public ServiceStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ServiceType ServiceType { get; set; } = ServiceType.DockerImage;
    public Guid ServiceId { get; set; }
}

