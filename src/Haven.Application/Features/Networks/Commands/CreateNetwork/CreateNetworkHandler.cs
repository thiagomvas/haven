using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Networks.Commands.CreateNetwork;

public sealed class CreateNetworkHandler(
    INetworkRepository networkRepository,
    IUnitOfWork unitOfWork) : Common.Messaging.ICommandHandler<CreateNetworkCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateNetworkCommand request, CancellationToken cancellationToken)
    {
        var network = Network.Create(
            request.Name,
            DetermineNetworkType(request.ProjectId, request.EnvironmentId),
            request.ProjectId,
            request.EnvironmentId,
            request.Metadata);

        await networkRepository.AddAsync(network, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(network.Id);
    }

    private static NetworkType DetermineNetworkType(Guid? projectId, Guid? environmentId)
    {
        return (projectId, environmentId) switch
        {
            (not null, not null) => NetworkType.ProjectEnvironment,
            (null, null) => NetworkType.Shared,
            _ => NetworkType.Shared
        };
    }
}
