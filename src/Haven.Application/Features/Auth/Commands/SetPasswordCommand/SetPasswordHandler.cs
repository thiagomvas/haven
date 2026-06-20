using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Auth.Commands.SetPasswordCommand;

public sealed class SetPasswordHandler(IAuthService authService, ICurrentUserService currentUserService)
    : ICommandHandler<SetPasswordCommand>
{
    public async ValueTask<Result> Handle(SetPasswordCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        if (userId is null)
            return Error.Unauthorized;

        var result = await authService.SetPasswordAsync(userId.Value, command.NewPassword);
        return result.IsFailure ? result.Error : Result.Success();
    }
}