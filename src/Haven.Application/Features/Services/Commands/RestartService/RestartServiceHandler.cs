using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Services.Commands.RestartService;

public class RestartServiceHandler(
    IProjectRepository projectRepository,
    IDeployServiceFactory deployServiceFactory)
    : Haven.Application.Common.Messaging.ICommandHandler<RestartServiceCommand>
{
    public async ValueTask<Result> Handle(RestartServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var service = environment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (service is null)
            return Error.NotFoundFor(nameof(Haven.Domain.Entities.Service), request.ServiceId);

        var deployService = deployServiceFactory.Create(service);
        var restartResult = await deployService.RestartAsync(service, cancellationToken);

        if (restartResult.IsFailure)
            return restartResult;

        return Result.Success();
    }
}
