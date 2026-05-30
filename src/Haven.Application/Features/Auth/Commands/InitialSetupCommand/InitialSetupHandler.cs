using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Auth.Commands.InitialSetupCommand;

public class InitialSetupHandler(IAuthService authService, IHavenService havenService)
    : ICommandHandler<InitialSetupCommand, AuthResponse>
{
    public async ValueTask<Result<AuthResponse>> Handle(InitialSetupCommand command, CancellationToken cancellationToken)
    {
        if (!await havenService.RequiresFirstTimeSetupAsync(cancellationToken))
            return Error.Failure("Setup.NotRequired", "Setup has already been completed.");

        return await authService.RegisterAsync(command.Name, command.Email, command.Password);
    }
}
