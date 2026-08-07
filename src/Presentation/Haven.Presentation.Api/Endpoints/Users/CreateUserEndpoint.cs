using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Users;
using Haven.Application.Features.Users.Commands.CreateUser;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Users;

public sealed class CreateUserEndpoint(IMediator mediator) : Endpoint<CreateUserCommand, ApiResponse<UserDto>>
{
    public override void Configure()
    {
        Post("/users");
        Options(x => x.WithTags("Users"));
        Summary(s =>
        {
            s.Summary = "Create a user";
            s.Description = "Invites a new user by email. Sends a first-access link so the invitee sets their own name and password; requires a system-default SMTP provider to be configured.";
            s[201] = "User created and invite email enqueued";
            s[400] = "Validation error, or no default SMTP provider is configured";
            s[403] = "Forbidden";
            s[409] = "Email already in use";
        });
    }

    public override async Task HandleAsync(CreateUserCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}