using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Environments.Commands.DeleteEnvironment;

public sealed class DeleteEnvironmentHandler(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteEnvironmentCommand>
{
    public async ValueTask<Result> Handle(DeleteEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithEnvironmentsAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor("Environment", request.EnvironmentId);

        project.RemoveEnvironment(request.EnvironmentId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
