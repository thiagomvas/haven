using FastEndpoints;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.RegisterCommand;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Setup;

public sealed class RegisterEndpoint(IMediator mediator) : Endpoint<RegisterCommand, ApiResponse<AuthResponse>>
{
    public override void Configure()
    {
        Post("/setup/register");
        AllowAnonymous();
        Options(x => x.WithTags("Setup"));
        Summary(s =>
        {
            s.Summary = "Register first user";
            s.Description = "Creates the initial admin account during first-time setup.";
            s[200] = "Account created";
            s[400] = "Validation error";
            s[409] = "Setup already completed";
        });
    }

    public override async Task HandleAsync(RegisterCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
