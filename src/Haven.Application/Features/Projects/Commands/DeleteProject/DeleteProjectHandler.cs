using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectHandler(
    IProjectRepository repository) : ICommandHandler<DeleteProjectCommand>
{
    public async ValueTask<Result> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
            return Error.NotFoundFor("Project", request.Id);

        project.Delete();
        repository.Remove(project);

        return Result.Success();
    }
}