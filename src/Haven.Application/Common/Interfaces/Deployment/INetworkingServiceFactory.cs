using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface INetworkingServiceFactory
{
    INetworkingService? Create(ServiceType type);
}