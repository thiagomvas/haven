using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.CreateNetwork;
using Haven.Domain.Aggregates;
using Mediator;


namespace Haven.Application.Features.Environments.Commands.CreateEnvironment;

public sealed class CreateEnvironmentHandler(
    IProjectRepository projectRepository,
    IEnvironmentRepository environmentRepository,
    INetworkRepository networkRepository)
    : Common.Messaging.ICommandHandler<CreateEnvironmentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        if (project.Environments.Any(e => string.Equals(e.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor("Environment", request.Name);

        var environment = project.AddEnvironment(request.Name, request.Description);
        environmentRepository.AddAsync(environment, cancellationToken: cancellationToken);

        var network = Network.CreateProjectEnvironmentNetwork(
            project.Id,
            project.Name,
            environment.Id,
            environment.Name);
        await networkRepository.AddAsync(network, cancellationToken);

        return Result<Guid>.CreatedFor(environment.Id);
    }
}