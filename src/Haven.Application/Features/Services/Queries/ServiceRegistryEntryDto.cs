using Haven.Domain;

namespace Haven.Application.Features.Services.Queries;

public sealed class ServiceRegistryEntryDto
{
    public string? ContainerName { get; set; }
    public string? IpAddress { get; set; }
    public List<PortMappingDto> Ports { get; set; } = [];
    public ServiceStatus Status { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
