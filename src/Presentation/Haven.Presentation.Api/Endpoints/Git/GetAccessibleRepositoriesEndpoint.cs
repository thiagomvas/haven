using FastEndpoints;

using Haven.Application.Common.Models;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Git.Queries.GetAccessibleRepositories;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Git;

public class GetAccessibleRepositoriesEndpoint(IMediator mediator) : Endpoint<GetAccessibleRepositoriesQuery, ApiResponse<IReadOnlyList<GitRepositorySummary>>>
{
    public override void Configure()
    {
        Get("/git/repositories");

        Options(x => x.WithTags("Git"));
        Summary(s =>
        {
            s.Summary = "Get accessible repositories";
            s.Description = "Returns repositories the given git credential owns or has access to. Currently only supported for GitHub credentials; other providers return an empty list.";
            s[200] = "List of repositories";
            s[400] = "Validation error";
            s[404] = "Git credentials not found";
        });
    }

    public override async Task HandleAsync(GetAccessibleRepositoriesQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}