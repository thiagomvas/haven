using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Commands.DeleteCustomAction;

public sealed class DeleteCustomActionHandler(ICustomActionRepository repository)
    : ICommandHandler<DeleteCustomActionCommand>
{
    public async ValueTask<Result> Handle(DeleteCustomActionCommand command, CancellationToken cancellationToken)
    {
        var action = await repository.GetByIdAsync(command.ActionId, cancellationToken);
        if (action is null || action.ServiceId != command.ServiceId)
            return Error.NotFoundFor(nameof(CustomAction), command.ActionId);

        await repository.RemoveAsync(action, cancellationToken);

        return Result.Success();
    }
}
