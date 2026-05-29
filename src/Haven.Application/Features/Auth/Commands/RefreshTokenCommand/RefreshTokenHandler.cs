using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.RefreshTokenCommand;

public class RefreshTokenHandler(IAuthService authService) : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async ValueTask<Result<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        return await authService.RefreshAsync(command.Token);
    }
}
