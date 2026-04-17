namespace Haven.Application.Features.Environments;

public sealed class EnvironmentManifestDto
{
    public required Guid Id { get; init; }
    public required Guid ProjectId { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
    public required string NetworkName { get; init; }
}
