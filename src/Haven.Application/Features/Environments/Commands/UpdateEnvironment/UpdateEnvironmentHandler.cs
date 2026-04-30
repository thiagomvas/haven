using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;


namespace Haven.Application.Features.Environments.Commands.UpdateEnvironment;

public sealed class UpdateEnvironmentHandler(
    IProjectRepository projectRepository) : ICommandHandler<UpdateEnvironmentCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(UpdateEnvironmentCommand request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetByIdWithEnvironmentsAsync(request.ProjectId, cancellationToken);
        if (project is null)
            return Error.NotFoundFor(nameof(Project), request.ProjectId);

        var environment = project.Environments.FirstOrDefault(e => e.Id == request.EnvironmentId);
        if (environment is null)
            return Error.NotFoundFor("Environment", request.EnvironmentId);

        if (request.Name.HasValue && !string.Equals(request.Name.Value, environment.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameConflict = project.Environments.Any(e =>
                e.Id != request.EnvironmentId &&
                string.Equals(e.Name, request.Name.Value, StringComparison.OrdinalIgnoreCase));
            if (nameConflict)
                return Error.ConflictFor("Environment", request.Name.Value);
        }

        project.UpdateEnvironment(request.EnvironmentId, request.Name, request.Description);

        return Result<Guid>.Success(request.EnvironmentId);
    }
}
