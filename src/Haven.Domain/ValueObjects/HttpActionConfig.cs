using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

public sealed record HttpActionConfig(
    [property: JsonConverter(typeof(HttpMethodJsonConverter))]
    HttpMethod Method,
    string Url,
    IReadOnlyDictionary<string, string> Headers,
    string? Body,
    IReadOnlyList<int>? SuccessStatusCodes) : ActionConfig;