using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public sealed class SetEnvForServiceHandler(IServiceRepository repository, IEnvironmentVariableService environmentVariableService) : ICommandHandler<SetEnvForServiceCommand>
{
    public async ValueTask<Result> Handle(SetEnvForServiceCommand command, CancellationToken cancellationToken)
    {
        var service = await repository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null) return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var siblings = await repository.GetByEnvironmentIdAsync(service.EnvironmentId, cancellationToken);
        var contents = new Dictionary<Guid, string>();
        foreach (var sibling in siblings.Where(s => s.Id != service.Id))
            contents[sibling.Id] = await environmentVariableService.BuildEnvFileForServiceDirectAsync(sibling.Id, cancellationToken);

        var error = ServiceReferenceValidator.Validate(service, command.EnvFile, siblings, contents);
        if (error is not null) return error;

        await environmentVariableService.SetEnvironmentVariablesFromFileForServiceAsync(command.ServiceId, command.EnvFile, cancellationToken);
        service.UpdateEnvironmentVariables();
        return Result.Success();
    }
}