namespace Haven.Application.Features.Projects;

/// <summary>
/// YAML-serializable representation of a project for manifest files.
/// Used to persist and version control project definitions alongside infrastructure code.
/// Maps to and from Project domain entities for synchronization with the database.
/// </summary>
public sealed class ProjectManifestDto
{
    /// <summary>Unique identifier for the project.</summary>
    public required Guid Id { get; init; }

    /// <summary>Human-readable name of the project.</summary>
    public required string Name { get; init; }

    /// <summary>Short alias for Docker resource naming (2–8 chars, lowercase alphanumeric/hyphens).</summary>
    public string? Alias { get; init; }

    /// <summary>Optional description of the project's purpose.</summary>
    public required string? Description { get; init; }
}
