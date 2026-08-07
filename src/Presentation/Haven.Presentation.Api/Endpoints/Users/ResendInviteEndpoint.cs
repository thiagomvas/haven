using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Users.Commands.ResendInvite;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class ResendInviteEndpoint(IMediator mediator) : Endpoint<ResendInviteCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/users/{id}/resend-invite");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Resend a user's invite";
            s.Description = "Revokes any active invite token for a still-pending user and sends a new invite email.";
            s[200] = "Invite resent";
            s[400] = "User has already completed first access, or no default SMTP provider is configured";
            s[403] = "Forbidden";
            s[404] = "User not found";
        });
    }

    public override async Task HandleAsync(ResendInviteCommand req, CancellationToken ct)
    {
        req.UserId = Route<Guid>("id");
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}