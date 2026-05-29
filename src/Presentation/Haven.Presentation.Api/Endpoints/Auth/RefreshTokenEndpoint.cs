using FastEndpoints;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.RefreshTokenCommand;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class RefreshTokenEndpoint(IMediator mediator) : Endpoint<RefreshTokenCommand, ApiResponse<AuthResponse>>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Refresh token";
            s.Description = "Issues a new access token and rotated refresh token. The submitted refresh token is invalidated.";
            s[200] = "Token refreshed";
            s[400] = "Validation error";
            s[401] = "Invalid or expired refresh token";
        });
    }

    public override async Task HandleAsync(RefreshTokenCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
