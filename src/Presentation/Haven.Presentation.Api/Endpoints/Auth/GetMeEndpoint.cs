using System.Security.Claims;
using FastEndpoints;
using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Queries.GetMe;
using Haven.Presentation.Api.Extensions;
using Mediator;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class GetMeEndpoint(IMediator mediator) : EndpointWithoutRequest<ApiResponse<MeResponse>>
{
    public override void Configure()
    {
        Get("/auth/me");
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Get current user";
            s.Description = "Returns the authenticated user's profile from the database.";
            s[200] = "Current user";
            s[401] = "Unauthorized";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await mediator.Send(new GetMeQuery { UserId = userId }, ct);
        await this.SendResultAsync(result, ct);
    }
}
