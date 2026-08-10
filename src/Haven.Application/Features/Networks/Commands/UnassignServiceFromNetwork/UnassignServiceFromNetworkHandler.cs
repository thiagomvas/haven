using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Networks.Commands.UnassignServiceFromNetwork;

public sealed class UnassignServiceFromNetworkHandler(
    INetworkRepository networkRepository,
    IServiceRepository serviceRepository,
    INetworkingServiceFactory networkingServiceFactory) : ICommandHandler<UnassignServiceFromNetworkCommand>
{
    public async ValueTask<Result> Handle(UnassignServiceFromNetworkCommand request, CancellationToken cancellationToken)
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

        return await networkingService.DisconnectServiceFromNetworksAsync(service.Id, [network.Id], cancellationToken);
    }
}