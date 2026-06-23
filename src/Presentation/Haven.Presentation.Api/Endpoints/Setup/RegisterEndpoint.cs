using FastEndpoints;

using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.InitialSetupCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed class InitialSetupEndpoint(IMediator mediator) : Endpoint<InitialSetupCommand, ApiResponse<AuthResponse>>
{
    public override void Configure()
    {
        Post("/setup/register");
        
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Initial admin setup";
            s.Description = "Creates the initial admin account during first-time setup. Can only be called once.";
            s[200] = "Account created";
            s[400] = "Validation error";
            s[409] = "Setup already completed";
        });
    }

    public override async Task HandleAsync(InitialSetupCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}