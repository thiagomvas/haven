using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.GitCredentials.Commands.UpdateGitCredentials;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public sealed class UpdateGitCredentialsEndpoint(IMediator mediator) : Endpoint<UpdateGitCredentialsCommand, ApiResponse<Guid>>
{
    public override void Configure()
    {
        Patch("/credentials/{id}");
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Update git credentials";
            s.Description = "Partially updates a git credential's display name or active state.";
            s[200] = "Updated";
            s[400] = "Validation error";
            s[404] = "Credentials not found";
            s[409] = "Credentials with this display name already exist";
        });
    }

    public override async Task HandleAsync(UpdateGitCredentialsCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}
