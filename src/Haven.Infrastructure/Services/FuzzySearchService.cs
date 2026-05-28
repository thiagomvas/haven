using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;

namespace Haven.Infrastructure.Services;

public class FuzzySearchService : IFuzzySearchService
{
    private readonly IEnumerable<IFuzzySearchableRepository> _repositories;

    public FuzzySearchService(IEnumerable<IFuzzySearchableRepository> repositories)
    {
        _repositories = repositories;
    }

    public async Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, int count = 10, CancellationToken cancellationToken = default)
    {
        var results = await Task.WhenAll(_repositories.Select(repo => repo.FuzzySearchAsync(query, cancellationToken)));
        
        return results.SelectMany(r => r)
            .Take(count)
            .OrderByDescending(r => r.Similarity);
    }
}