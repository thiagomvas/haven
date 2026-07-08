using Haven.Domain;

namespace Haven.Application.Features.Services;

/// <summary>
/// YAML-serializable representation of a service volume for manifest files. All volumes are
/// written regardless of <see cref="BackupEnabled"/>; that flag only governs whether a managed
/// volume's file contents are additionally stored alongside the service manifest under
/// <c>volumes/{Name}/</c>.
/// </summary>
public sealed class VolumeManifest
{
    public Guid Id { get; set; }
    public VolumeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string Target { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public bool BackupEnabled { get; set; }
}