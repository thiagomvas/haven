namespace Haven.Application.Common.Interfaces.Repositories;

public interface IFuzzySearchableRepository
{
    string EntityType { get; }
    Task<IEnumerable<FuzzySearchResult>> FuzzySearchAsync(string query, CancellationToken cancellationToken);
}