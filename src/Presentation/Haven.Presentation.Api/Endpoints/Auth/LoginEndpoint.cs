using FastEndpoints;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.LoginCommand;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class LoginEndpoint(IMediator mediator) : Endpoint<LoginCommand, ApiResponse<AuthResponse>>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Login";
            s.Description = "Authenticates a user and returns an access token and refresh token.";
            s[200] = "Authenticated";
            s[400] = "Validation error";
            s[401] = "Invalid credentials";
        });
    }

    public override async Task HandleAsync(LoginCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
