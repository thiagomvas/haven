using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Business.Queries.FuzzySearch;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints;

public class FuzzySearchEndpoint(IMediator mediator) : Endpoint<FuzzySearchQuery, ApiResponse<IEnumerable<FuzzySearchResult>>>
{
    public override void Configure()
    {
        Get("/fuzzy");

        Options(x => x.WithTags("Search"));
        Summary(s =>
        {
            s.Summary = "Fuzzy search across Haven resources";
            s.Description = "Searches projects, environments, services and other resources by approximate name match. Use Query for the search text, Count to cap the number of results (default 10) and Scopes to restrict the resource types searched.";
            s[200] = "Matching results, ranked by relevance";
            s[400] = "Validation error";
        });
    }

    public override async Task HandleAsync(FuzzySearchQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}