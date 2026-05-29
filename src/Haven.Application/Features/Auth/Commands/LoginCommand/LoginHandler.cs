using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.LoginCommand;

public class LoginHandler(IAuthService authService) : ICommandHandler<LoginCommand, AuthResponse>
{
    public async ValueTask<Result<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        return await authService.LoginAsync(command.Email, command.Password);
    }
}
