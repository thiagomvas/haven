using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Application.Features.Networks.Commands.CreateNetwork;

public sealed class CreateNetworkHandler(
    INetworkRepository networkRepository,
    IProjectRepository projectRepository,
    INetworkingServiceFactory networkingServiceFactory,
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

        if (network.Type == NetworkType.ProjectEnvironment &&
            request.ProjectId is not null && request.EnvironmentId is not null)
        {
            var project = await projectRepository.GetByIdAsync(request.ProjectId.Value, cancellationToken);
            var environment = project?.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId.Value);
            if (project is null || environment is null)
                return Error.NotFoundFor(project is null ? nameof(Project) : nameof(Environment),
                    request.ProjectId ?? request.EnvironmentId ?? Guid.Empty);
        }
        else if (network.Type == NetworkType.Shared)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var networkingService = networkingServiceFactory.Create(ServiceType.DockerImage);
            if (networkingService is not null)
            {
                var ensureResult = await networkingService.EnsureNetworkExistsAsync(network.Id, cancellationToken);
                if (ensureResult.IsFailure)
                    return ensureResult.Error;
            }
        }

        return Result<Guid>.CreatedFor(network.Id);
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