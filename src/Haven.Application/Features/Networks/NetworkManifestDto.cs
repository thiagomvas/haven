namespace Haven.Application.Features.Networks;

public sealed record NetworkManifestDto
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public string? Metadata { get; init; }
}
