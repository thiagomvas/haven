using FastEndpoints;

using Haven.Application.Common.Models;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Backups.Queries.ListGitCommits;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Backups;

public sealed class ListGitCommitsEndpoint(IMediator mediator)
    : EndpointWithoutRequest<ApiResponse<IReadOnlyList<GitCommitInfo>>>
{
    public override void Configure()
    {
        Get("/backups/commits");
        Options(x => x.WithTags("Backups"));
        Summary(s =>
        {
            s.Summary = "List git backup commits";
            s.Description = "Returns the most recent commits from the local manifests git repository.";
            s[200] = "Success";
            s[403] = "Forbidden";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await mediator.Send(new ListGitCommitsQuery(), ct);
        await this.SendResultAsync(result, ct);
    }
}
