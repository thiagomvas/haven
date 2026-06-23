using FastEndpoints;

using Haven.Application.Common;
using Haven.Application.Common.Responses;
using Haven.Application.Features.Business.Queries.FuzzySearch;
using Haven.Presentation.Api.Extensions;

using Mediator;

namespace Haven.Presentation.Api.Endpoints.Business;

public class FuzzySearchEndpoint(IMediator mediator) : Endpoint<FuzzySearchQuery, ApiResponse<IEnumerable<FuzzySearchResult>>>
{
    public override void Configure()
    {
        Get("/fuzzy");
        
    }

    public override async Task HandleAsync(FuzzySearchQuery req, CancellationToken ct)
    {
        var result = await mediator.Send(req, ct);
        await this.SendResultAsync(result, ct);
    }
}