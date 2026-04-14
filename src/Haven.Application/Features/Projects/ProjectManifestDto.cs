namespace Haven.Application.Dtos;

public sealed class ProjectManifestDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string? Description { get; init; }
}
