using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

public class SetEnvForProjectHandler(IEnvironmentVariableService service) : ICommandHandler<SetEnvForProjectCommand>
{
    public async ValueTask<Result> Handle(SetEnvForProjectCommand command, CancellationToken cancellationToken)
    {
        await service.SetEnvironmentVariablesFromFileForProjectAsync(command.ProjectId, command.EnvFile, cancellationToken);
        return Result.Success();
    }
}