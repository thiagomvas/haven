using FastEndpoints;

using Haven.Application.Common.Responses;
using Haven.Application.Features.Git.Queries.GetRemoteBranches;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Git;

public class GetRemoteBranchesEndpoint(IMediator mediator) : Endpoint<GetRemoteBranchesQuery, ApiResponse<IReadOnlyList<string>>>
{
    public override void Configure()
    {
        Get("/git/branches");
        AllowAnonymous();
        Options(x => x.WithTags("Git"));
        Summary(s =>
        {
            s.Summary = "Get remote branches";
            s.Description = "Returns all branches from a remote git repository. Optionally authenticates using stored git credentials.";
            s[200] = "List of branch names";
            s[400] = "Validation error";
            s[404] = "Git credentials not found";
        });
    }

    public override async Task HandleAsync(GetRemoteBranchesQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}