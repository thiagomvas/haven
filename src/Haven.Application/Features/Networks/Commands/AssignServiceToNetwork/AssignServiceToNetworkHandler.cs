using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Networks.Commands.AssignServiceToNetwork;

public sealed class AssignServiceToNetworkHandler(
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository,
    INetworkingServiceFactory networkingServiceFactory) : ICommandHandler<AssignServiceToNetworkCommand>
{
    public async ValueTask<Result> Handle(AssignServiceToNetworkCommand request, CancellationToken cancellationToken)
    {
        var network = await networkRepository.GetByIdAsync(request.NetworkId, cancellationToken);
        if (network is null)
            return Error.NotFoundFor(nameof(Network), request.NetworkId);

        var service = await serviceRepository.GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), request.ServiceId);

        var networkingService = networkingServiceFactory.Create(ServiceType.DockerImage);
        if (networkingService is null)
            return Error.NotSupported;

        return await networkingService.ConnectServiceToNetworksAsync(service.Id, [network.Id], cancellationToken);
    }
}
