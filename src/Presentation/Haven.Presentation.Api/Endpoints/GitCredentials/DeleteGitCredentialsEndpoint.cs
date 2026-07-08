using FastEndpoints;

using Haven.Application.Features.GitCredentials.Commands.DeleteGitCredentials;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.GitCredentials;

public sealed class DeleteGitCredentialsEndpoint(IMediator mediator) : Endpoint<DeleteGitCredentialsCommand>
{
    public override void Configure()
    {
        Delete("/credentials/{id}");
        Options(x => x.WithTags("Git Credentials"));
        Summary(s =>
        {
            s.Summary = "Delete git credentials";
            s.Description = "Permanently deletes a git credential by ID.";
            s[204] = "Deleted";
            s[404] = "Credentials not found";
        });
    }

    public override async Task HandleAsync(DeleteGitCredentialsCommand req, CancellationToken ct)
    {
        req.Id = Route<Guid>("id");

        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}