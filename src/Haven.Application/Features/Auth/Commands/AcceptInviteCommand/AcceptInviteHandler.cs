using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.AcceptInviteCommand;

public class AcceptInviteHandler(IAuthService authService) : ICommandHandler<AcceptInviteCommand, AuthResponse>
{
    public async ValueTask<Result<AuthResponse>> Handle(AcceptInviteCommand command, CancellationToken cancellationToken)
    {
        return await authService.AcceptInviteAsync(command.Token, command.Name, command.Password);
    }
}
