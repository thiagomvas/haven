namespace Haven.Application.Common.Interfaces.Repositories;

public interface IFuzzySearchableRepository
{
    Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, CancellationToken cancellationToken);
}