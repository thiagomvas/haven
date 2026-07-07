using Haven.Domain;

namespace Haven.Application.Features.Services;

/// <summary>
/// YAML-serializable representation of a service volume for manifest files. Only volumes with
/// backup enabled are written. For managed volumes, the file contents are stored alongside the
/// service manifest under <c>volumes/{Name}/</c>.
/// </summary>
public sealed class VolumeManifest
{
    public Guid Id { get; set; }
    public VolumeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string Target { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
}
