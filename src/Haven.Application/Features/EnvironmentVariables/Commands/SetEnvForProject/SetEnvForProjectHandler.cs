using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

public class SetEnvForProjectHandler(IProjectRepository repository, IEnvironmentVariableService environmentVariableService) : ICommandHandler<SetEnvForProjectCommand>
{
    public async ValueTask<Result> Handle(SetEnvForProjectCommand command, CancellationToken cancellationToken)
    {
        var project = await repository.GetByIdAsync(command.ProjectId, cancellationToken);
        if (project is null) return Error.NotFoundFor(nameof(Project), command.ProjectId);
        
        await environmentVariableService.SetEnvironmentVariablesFromFileForProjectAsync(command.ProjectId, command.EnvFile, cancellationToken);
        project.UpdateEnvironmentVariables();
        return Result.Success();
    }
}