using Haven.Application.Common;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Messaging;
using Haven.Application.Configuration;

namespace Haven.Application.Features.Auth.Commands.InitialSetupCommand;

public class InitialSetupHandler(IAuthService authService, IHavenService havenService)
    : ICommandHandler<InitialSetupCommand, AuthResponse>
{
    public async ValueTask<Result<AuthResponse>> Handle(InitialSetupCommand command, CancellationToken cancellationToken)
    {
        var stage = await havenService.GetSetupStageAsync(cancellationToken);
        if (stage != SetupStage.InstanceConfigured)
            return Error.InvalidOperation("Initial setup can only be performed after instance configuration.");

        var result = await authService.RegisterAsync(command.Name, command.Email, command.Password);
        if (result.IsSuccess)
            await havenService.AdvanceSetupStageAsync(SetupStage.SuperUserCreated, cancellationToken);

        return result;
    }
}