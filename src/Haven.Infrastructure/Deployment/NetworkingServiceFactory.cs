using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;

namespace Haven.Infrastructure.Deployment;

public class NetworkingServiceFactory : INetworkingServiceFactory
{
    private readonly IEnumerable<INetworkingService> services;

    public NetworkingServiceFactory(IEnumerable<INetworkingService> services)
    {
        this.services = services;
    }

    public INetworkingService? Create(ServiceType type)
    {
        return services.FirstOrDefault(s => s.ServiceType == type);
    }
}