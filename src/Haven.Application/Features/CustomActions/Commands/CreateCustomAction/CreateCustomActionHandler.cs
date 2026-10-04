using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Commands.CreateCustomAction;

public sealed class CreateCustomActionHandler(
    IServiceRepository serviceRepository,
    ICustomActionRepository repository) : ICommandHandler<CreateCustomActionCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateCustomActionCommand command, CancellationToken cancellationToken)
    {
        var service = await serviceRepository.GetByIdAsync(command.ServiceId, cancellationToken);
        if (service is null)
            return Error.NotFoundFor(nameof(Service), command.ServiceId);

        var existing = await repository.GetForServiceAsync(command.ServiceId, cancellationToken);
        if (existing.Any(a => string.Equals(a.ActionName, command.ActionName, StringComparison.OrdinalIgnoreCase)))
            return Error.ConflictFor(nameof(CustomAction), command.ActionName);

        var action = CustomAction.Create(
            command.ServiceId,
            command.ActionName,
            command.Alias,
            command.ActionDescription,
            command.Icon,
            command.Config,
            command.RequiredPermissions,
            command.Risk,
            command.Timeout,
            command.Inputs);

        await repository.AddAsync(action, cancellationToken);

        return Result<Guid>.CreatedFor(action.Id);
    }
}
