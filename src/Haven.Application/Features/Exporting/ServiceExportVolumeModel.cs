using Haven.Domain.Enums;

namespace Haven.Application.Features.Exporting;

/// <summary>
/// Intermediate representation of a single volume mount on an exported service.
/// </summary>
public class ServiceExportVolumeModel
{
    public VolumeType Type { get; set; }

    /// <summary>
    /// Host path, Docker volume name, or (for <see cref="VolumeType.Managed"/>) the resolved
    /// managed-volume directory on the host. Never <see langword="null"/> once resolved for export.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path inside the container where the volume is mounted.
    /// </summary>
    public string Target { get; set; } = string.Empty;

    public bool ReadOnly { get; set; }
}
