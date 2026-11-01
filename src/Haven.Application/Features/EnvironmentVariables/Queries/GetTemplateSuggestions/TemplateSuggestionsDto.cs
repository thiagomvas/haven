namespace Haven.Application.Features.EnvironmentVariables.Queries.GetTemplateSuggestions;

public sealed class TemplateSuggestionsDto
{
    /// <summary>The namespaces that can be referenced from the edited scope.</summary>
    public IReadOnlyList<string> Namespaces { get; init; } = [];

    /// <summary>The known keys per namespace. For <c>services</c>, keys have the form <c>alias.scope.key</c>.</summary>
    public IReadOnlyList<TemplateSuggestionDto> Variables { get; init; } = [];

    /// <summary>The filters usable after a <c>|</c>.</summary>
    public IReadOnlyList<string> Filters { get; init; } = [];

    /// <summary>
    /// Whether <see cref="Variables"/> is the complete set of resolvable keys (true for services). For broader scopes,
    /// narrower scopes may define more keys, so a key missing from the list is not necessarily invalid.
    /// </summary>
    public bool IsExhaustive { get; init; }
}

public sealed class TemplateSuggestionDto
{
    public string Namespace { get; init; } = default!;
    public string Key { get; init; } = default!;
    public string? Description { get; init; }
}
