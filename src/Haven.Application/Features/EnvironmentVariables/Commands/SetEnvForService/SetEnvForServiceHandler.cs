using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public class SetEnvForServiceHandler(IServiceRepository repository, IEnvironmentVariableService environmentVariableService) : ICommandHandler<SetEnvForServiceCommand>
{
    public async ValueTask<Result> Handle(SetEnvForServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null) return Error.NotFoundFor(nameof(Service), command.ServiceId);

        await environmentVariableService.SetEnvironmentVariablesFromFileForServiceAsync(command.ServiceId, command.EnvFile, cancellationToken);
        service.UpdateEnvironmentVariables();
        return Result.Success();
    }
}