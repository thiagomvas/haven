using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Networks.Commands.CreateNetwork;

public sealed class CreateNetworkHandler(
    INetworkRepository networkRepository,
    IProjectRepository projectRepository,
    IManifestSerializer manifestSerializer) : Common.Messaging.ICommandHandler<CreateNetworkCommand, Guid>
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

        // Save network manifest for ProjectEnvironment networks
        if (network.Type == NetworkType.ProjectEnvironment &&
            request.ProjectId is not null && request.EnvironmentId is not null)
        {
            var project = await projectRepository.GetByIdWithEnvironmentsAsync(request.ProjectId.Value, cancellationToken);
            if (project is not null)
            {
                var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId.Value);
                if (environment is not null)
                {
                    await manifestSerializer.WriteNetworkAsync(project, environment, network, cancellationToken);
                }
            }
        }

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
