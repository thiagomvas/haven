using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.CustomActions.Abstractions;
using Haven.Domain.Entities;

namespace Haven.Application.Features.CustomActions.Commands.ExecuteCustomAction;

public sealed class ExecuteCustomActionHandler(ICustomActionRepository repository, IActionRunner runner)
    : ICommandHandler<ExecuteCustomActionCommand>
{
    public async ValueTask<Result> Handle(ExecuteCustomActionCommand command, CancellationToken cancellationToken)
    {
        var action = await repository.GetByIdAsync(command.ActionId, cancellationToken);
        if (action is null || action.ServiceId != command.ServiceId)
            return Error.NotFoundFor(nameof(CustomAction), command.ActionId);

        return await runner.RunActionAsync(action.Id, command.Inputs, cancellationToken);
    }
}
