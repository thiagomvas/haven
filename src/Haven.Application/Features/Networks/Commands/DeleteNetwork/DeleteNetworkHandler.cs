using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Networks.Commands.DeleteNetwork;

public sealed class DeleteNetworkHandler(
    INetworkRepository networkRepository,
    INetworkingServiceFactory networkingServiceFactory) : ICommandHandler<DeleteNetworkCommand>
{
    public async ValueTask<Result> Handle(DeleteNetworkCommand request, CancellationToken cancellationToken)
    {
        var network = await networkRepository.GetByIdAsync(request.NetworkId, cancellationToken);
        if (network is null)
            return Error.NotFoundFor(nameof(Network), request.NetworkId);

        if (network.Type == NetworkType.ProjectEnvironment)
            return Error.InvalidOperation(
                "Project/environment networks are managed automatically and cannot be deleted directly.");

        var networkingService = networkingServiceFactory.Create(ServiceType.DockerImage);
        if (networkingService is not null)
        {
            var serviceIds = network.ServiceNetworks.Select(sn => sn.ServiceId).ToList();
            foreach (var serviceId in serviceIds)
                await networkingService.DisconnectServiceFromNetworksAsync(serviceId, [network.Id], cancellationToken);

            var deleteResult = await networkingService.DeleteNetworkAsync(network.Id, cancellationToken);
            if (deleteResult.IsFailure)
                return deleteResult.Error;
        }

        network.Delete();
        await networkRepository.DeleteAsync(network.Id, cancellationToken);

        return Result.Success();
    }
}
