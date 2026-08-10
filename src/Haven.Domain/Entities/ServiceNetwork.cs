using Haven.Domain.Aggregates;

namespace Haven.Domain.Entities;

public sealed class ServiceNetwork : Entity
{
    public Guid ServiceId { get; private set; }
    public Guid NetworkId { get; private set; }
    public string? IpAddress { get; private set; }

    public Service? Service { get; internal set; }
    public Network? Network { get; internal set; }

    private ServiceNetwork() { }

    public static ServiceNetwork Create(Guid serviceId, Guid networkId)
    {
        return new ServiceNetwork
        {
            ServiceId = serviceId,
            NetworkId = networkId
        };
    }

    public static ServiceNetwork Reconstitute(Guid serviceId, Guid networkId, Service? service = null, Network? network = null, string? ipAddress = null)
    {
        return new ServiceNetwork
        {
            ServiceId = serviceId,
            NetworkId = networkId,
            Service = service,
            Network = network,
            IpAddress = ipAddress
        };
    }

    public void AssignIpAddress(string ipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);
        IpAddress = ipAddress;
    }

    public void ClearIpAddress()
    {
        IpAddress = null;
    }
}