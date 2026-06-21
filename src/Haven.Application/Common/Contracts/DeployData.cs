using System.Net;

using Haven.Domain.ValueObjects;

namespace Haven.Application.Common.Contracts;

public class DeployData
{
    public Guid ServiceId { get; set; }
    public IPAddress? IpAddress { get; set; }
    public List<PortMapping>? Ports { get; set; }
    public string? ContainerName { get; set; }
}