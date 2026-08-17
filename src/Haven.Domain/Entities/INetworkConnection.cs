namespace Haven.Domain.Entities;

/// <summary>
/// Common surface for a container-to-network membership row (<see cref="ServiceNetwork"/> or
/// <see cref="SidecarNetwork"/>), letting infrastructure code that treats Service/Sidecar deploys
/// generically operate on either without type-switching on the concrete entity.
/// </summary>
public interface INetworkConnection
{
    Guid NetworkId { get; }
    string? IpAddress { get; }

    void AssignIpAddress(string ipAddress);
    void ClearIpAddress();
}