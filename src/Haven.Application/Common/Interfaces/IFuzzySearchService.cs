namespace Haven.Application.Common.Interfaces;

public interface IFuzzySearchService
{
    Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, int count = 10, IReadOnlyCollection<string>? scopes = null, CancellationToken cancellationToken = default);
}