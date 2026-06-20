using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;

public class SetEnvForEnvironmentHandler(IEnvironmentRepository repository, IEnvironmentVariableService environmentVariableService) : ICommandHandler<SetEnvForEnvironmentCommand>
{
    public async ValueTask<Result> Handle(SetEnvForEnvironmentCommand command, CancellationToken cancellationToken)
    {
        var environment = await repository.GetByIdAsync(command.EnvironmentId, cancellationToken);
        if (environment is null) return Error.NotFoundFor(nameof(Environment), command.EnvironmentId);

        await environmentVariableService.SetEnvironmentVariablesFromFileForEnvironmentAsync(command.EnvironmentId, command.EnvFile, cancellationToken);
        environment.UpdateEnvironmentVariables();
        return Result.Success();
    }
}