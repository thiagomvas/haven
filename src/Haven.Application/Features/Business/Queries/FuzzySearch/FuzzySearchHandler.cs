using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Business.Queries.FuzzySearch;

public class FuzzySearchHandler(IFuzzySearchService service) : IQueryHandler<FuzzySearchQuery, IEnumerable<FuzzySearchResult>>
{
    public async ValueTask<Result<IEnumerable<FuzzySearchResult>>> Handle(FuzzySearchQuery query, CancellationToken cancellationToken)
    {
        var results = await service.FuzzySearchAsync(query.Query, query.Count, cancellationToken);

        return Result<IEnumerable<FuzzySearchResult>>.Success(results);
    }
}