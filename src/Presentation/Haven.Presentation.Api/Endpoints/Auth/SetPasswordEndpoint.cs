using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Auth.Commands.SetPasswordCommand;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Auth;

public sealed class SetPasswordEndpoint(IMediator mediator) : Endpoint<SetPasswordCommand, ApiResponse>
{
    public override void Configure()
    {
        Post("/auth/set-password");
        Options(x => x.WithTags("Auth"));
        Summary(s =>
        {
            s.Summary = "Set password";
            s.Description = "Sets a new password for the authenticated user and clears the require-password-change flag.";
            s[200] = "Password updated";
            s[400] = "Validation error";
            s[401] = "Unauthorized";
        });
    }

    public override async Task HandleAsync(SetPasswordCommand req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}