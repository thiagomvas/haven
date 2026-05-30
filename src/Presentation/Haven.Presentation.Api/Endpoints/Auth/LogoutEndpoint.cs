using System.Security.Claims;
using FastEndpoints;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Responses;
using Haven.Presentation.Api.Extensions;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class LogoutEndpoint(IAuthService authService) : EndpointWithoutRequest<ApiResponse>
{
    public override void Configure()
    {
        Post("/auth/logout");
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Logout";
            s.Description = "Revokes all refresh tokens for the current session.";
            s[200] = "Logged out";
            s[401] = "Unauthorized";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sessionIdClaim = User.FindFirstValue("sessionId");

        if (sessionIdClaim is null || !Guid.TryParse(sessionIdClaim, out var sessionId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await authService.LogoutAsync(sessionId);
        await this.SendResultAsync(result, ct);
    }
}
