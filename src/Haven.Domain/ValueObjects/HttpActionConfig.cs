namespace Haven.Domain.ValueObjects;

public sealed record HttpActionConfig(
    HttpMethod Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    IReadOnlyList<int>? SuccessStatusCodes) : ActionConfig;