namespace Haven.Application.Common;

public record FuzzySearchResult(
    string EntityType,
    Guid Id,
    string Label,
    double Similarity,
    IReadOnlyDictionary<string, string>? Metadata = null
);