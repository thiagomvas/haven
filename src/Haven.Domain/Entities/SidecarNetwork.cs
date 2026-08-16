using Haven.Domain.Aggregates;

namespace Haven.Domain.Entities;

public sealed class SidecarNetwork : Entity, INetworkConnection
{
    public Guid SidecarId { get; private set; }
    public Guid NetworkId { get; private set; }
    public string? IpAddress { get; private set; }

    public Sidecar? Sidecar { get; internal set; }
    public Network? Network { get; internal set; }

    private SidecarNetwork() { }

    public static SidecarNetwork Create(Guid sidecarId, Guid networkId)
    {
        return new SidecarNetwork
        {
            SidecarId = sidecarId,
            NetworkId = networkId
        };
    }

    public static SidecarNetwork Reconstitute(Guid sidecarId, Guid networkId, Sidecar? sidecar = null, Network? network = null, string? ipAddress = null)
    {
        return new SidecarNetwork
        {
            SidecarId = sidecarId,
            NetworkId = networkId,
            Sidecar = sidecar,
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
