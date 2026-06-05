using System.Net;

namespace Haven.Application.Common.Contracts;

public class DeployData
{
    public Guid ServiceId { get; set; }
    public IPAddress? IpAddress { get; set; }
    public int? Port { get; set; }
    public string? ContainerName { get; set; }
}