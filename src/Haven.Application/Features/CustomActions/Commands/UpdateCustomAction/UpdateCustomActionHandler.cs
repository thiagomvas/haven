using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain;
using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Commands.UpdateCustomAction;

public sealed class UpdateCustomActionHandler(ICustomActionRepository repository)
    : ICommandHandler<UpdateCustomActionCommand>
{
    public async ValueTask<Result> Handle(UpdateCustomActionCommand command, CancellationToken cancellationToken)
    {
        var action = await repository.GetByIdAsync(command.ActionId, cancellationToken);
        if (action is null || action.ServiceId != command.ServiceId)
            return Error.NotFoundFor(nameof(CustomAction), command.ActionId);

        if (command.ActionName is not null)
        {
            var siblings = await repository.GetForServiceAsync(command.ServiceId, cancellationToken);
            if (siblings.Any(a => a.Id != action.Id &&
                                  string.Equals(a.ActionName, command.ActionName, StringComparison.OrdinalIgnoreCase)))
                return Error.ConflictFor(nameof(CustomAction), command.ActionName);
        }

        action.Update(
            command.ActionName,
            command.Alias,
            command.ActionDescription,
            command.Icon,
            command.Config,
            command.RequiredPermissions,
            command.Risk.ToOptional(),
            command.Timeout.ToOptional(),
            command.Inputs);

        return Result.Success();
    }
}