using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Features.Services.Commands.DeployService;

public sealed class DeployServiceHandler(
    IProjectRepository projectRepository,
    IDeployServiceFactory deployServiceFactory,
    IUnitOfWork unitOfWork)
    : Haven.Application.Common.Messaging.ICommandHandler<DeployServiceCommand>
{
    public async ValueTask<Result> Handle(DeployServiceCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithServicesAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor(nameof(Environment), request.EnvironmentId);

        var service = environment.Services.FirstOrDefault(s => s.Id == request.ServiceId);
        if (service is null)
            return Error.NotFoundFor(nameof(Haven.Domain.Entities.Service), request.ServiceId);

        var deployService = deployServiceFactory.Create(service);
        var deployResult = await deployService.DeployAsync(service, cancellationToken);

        if (deployResult.IsFailure)
            return deployResult;

        project.DeployService(request.EnvironmentId, request.ServiceId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
