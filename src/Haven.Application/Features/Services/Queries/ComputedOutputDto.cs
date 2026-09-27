namespace Haven.Application.Features.Services.Queries;

/// <summary>
/// A computed output as shown on the service dashboard. <see cref="Preview"/> is the full value
/// for a non-secret output, or a masked value for a secret one — the plaintext secret is never
/// carried by this DTO; it's only returned by the dedicated reveal query/endpoint.
/// </summary>
public sealed class ComputedOutputDto
{
    public string Key { get; set; } = default!;
    public string Label { get; set; } = default!;
    public bool IsSecret { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public string? Preview { get; set; }
}