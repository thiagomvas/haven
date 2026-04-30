using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectHandler(
    IProjectRepository repository) : ICommandHandler<UpdateProjectCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(project), request.Id);

        if (request.Name.HasValue)
        {
            var nameConflict = await repository.ExistsWithNameAsync(request.Name.Value, request.Id, cancellationToken);
            if (nameConflict)
                return Error.ConflictFor(nameof(Project), request.Name.Value);
        }

        project.Update(request.Name, request.Description);

        return Result<Guid>.Success(project.Id);
    }
}