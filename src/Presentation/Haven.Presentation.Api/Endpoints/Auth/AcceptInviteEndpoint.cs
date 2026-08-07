using FastEndpoints;

using Haven.Application.Common.Contracts;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.AcceptInviteCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class AcceptInviteEndpoint(IMediator mediator) : Endpoint<AcceptInviteCommand, ApiResponse<AuthResponse>>
{
    public override void Configure()
    {
        Post("/auth/accept-invite");
        AllowAnonymous();
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Accept an invite";
            s.Description = "Completes first access for an invited user: sets their name and password, and logs them in.";
            s[200] = "Account activated and authenticated";
            s[400] = "Validation error, or invite link is invalid or has expired";
        });
    }

    public override async Task HandleAsync(AcceptInviteCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
