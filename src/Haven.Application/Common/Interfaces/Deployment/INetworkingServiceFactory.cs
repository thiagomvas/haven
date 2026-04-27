using Haven.Domain;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface INetworkingServiceFactory
{
    INetworkingService? Create(ServiceType type);
}