using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;

public class SetEnvForEnvironmentHandler(IEnvironmentVariableService service) : ICommandHandler<SetEnvForEnvironmentCommand>
{
    public async ValueTask<Result> Handle(SetEnvForEnvironmentCommand command, CancellationToken cancellationToken)
    {
        await service.SetEnvironmentVariablesFromFileForProjectAsync(command.EnvironmentId, command.EnvFile, cancellationToken);
        return Result.Success();
    }
}