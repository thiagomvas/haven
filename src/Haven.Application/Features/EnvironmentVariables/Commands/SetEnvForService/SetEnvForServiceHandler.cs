using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public class SetEnvForServiceHandler(IEnvironmentVariableService service) : ICommandHandler<SetEnvForServiceCommand>
{
    public async ValueTask<Result> Handle(SetEnvForServiceCommand command, CancellationToken cancellationToken)
    {
        await service.SetEnvironmentVariablesFromFileForProjectAsync(command.ServiceId, command.EnvFile, cancellationToken);
        return Result.Success();
    }
}