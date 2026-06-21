using System.Text.Json.Serialization;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Aggregates;

public class ServiceRegistryEntry : AggregateRoot
{
    public Guid ServiceId { get; set; }
    public string? ContainerName { get; set; }
    public string? IpAddress { get; set; }
    public List<PortMapping> Ports { get; set; } = [];
    public ServiceStatus Status { get; set; } = ServiceStatus.Unknown;
    public DateTime RegisteredAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    [JsonIgnore] public Service? Service { get; set; }
    
    public static ServiceRegistryEntry Create(Guid serviceId)
    {
        var now = DateTime.UtcNow;
        return new ServiceRegistryEntry
        {
            Id = Guid.NewGuid(),
            ServiceId = serviceId,
            RegisteredAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateFromService(Service service)
    {
        Status = service.Status;
        UpdatedAt = DateTime.UtcNow;
    }
    public void UpdateRuntime(string ip, List<PortMapping> ports, ServiceStatus status)
    {
        IpAddress = ip;
        Ports = ports;
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkStopped()
    {
        Status = ServiceStatus.Stopped;
        UpdatedAt = DateTime.UtcNow;
    }
}