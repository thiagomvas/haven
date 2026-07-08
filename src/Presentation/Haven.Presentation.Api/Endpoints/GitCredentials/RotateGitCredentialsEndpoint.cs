using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.GitCredentials.Commands.RotateGitCredentials;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public sealed class RotateGitCredentialsEndpoint(IMediator mediator) : Endpoint<RotateGitCredentialsCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Post("/credentials/{id}/rotate");
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Rotate git credential secret";
            s.Description = "Replaces the auth method and secret (token or SSH key) on an existing credential without recreating it.";
            s[200] = "Rotated";
            s[400] = "Validation error";
            s[404] = "Credentials not found";
        });
    }

    public override async Task HandleAsync(RotateGitCredentialsCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}