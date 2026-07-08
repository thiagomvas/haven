using System.Text.Json.Serialization;

using Haven.Domain.Exceptions;

namespace Haven.Domain.Entities;

public sealed class ServiceVolume : Entity
{
    public Guid ServiceId { get; set; }
    public VolumeType Type { get; set; }

    /// <summary>
    /// Logical name of the volume within the service. For <see cref="VolumeType.Named"/>
    /// this doubles as the display name; the Docker volume name lives in <see cref="Source"/>.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Host path for <see cref="VolumeType.HostPath"/>, Docker volume name for
    /// <see cref="VolumeType.Named"/>, and <c>null</c> for <see cref="VolumeType.Managed"/>
    /// (the managed directory is derived from the service and volume ids at deploy time).
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Absolute path inside the container where the volume is mounted.
    /// </summary>
    public string Target { get; set; } = default!;

    public bool ReadOnly { get; set; }

    /// <summary>
    /// When enabled, the volume definition (and, for managed volumes, its files) is
    /// included in manifests and backups. Off by default.
    /// </summary>
    public bool BackupEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [JsonIgnore] public Service? Service { get; set; }

    private ServiceVolume() { }

    public static ServiceVolume Create(
        Guid serviceId,
        VolumeType type,
        string name,
        string target,
        string? source = null,
        bool readOnly = false,
        bool backupEnabled = false)
    {
        name = name?.Trim() ?? string.Empty;
        target = target?.Trim() ?? string.Empty;
        source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();

        Validate(type, name, target, source);

        var now = DateTime.UtcNow;
        return new ServiceVolume
        {
            ServiceId = serviceId,
            Type = type,
            Name = name,
            Source = type == VolumeType.Managed ? null : source,
            Target = target,
            ReadOnly = readOnly,
            BackupEnabled = backupEnabled,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ServiceVolume Reconstitute(
        Guid id,
        Guid serviceId,
        VolumeType type,
        string name,
        string target,
        string? source,
        bool readOnly,
        bool backupEnabled,
        DateTime createdAt,
        DateTime updatedAt)
    {
        var volume = new ServiceVolume
        {
            ServiceId = serviceId,
            Type = type,
            Name = name,
            Source = source,
            Target = target,
            ReadOnly = readOnly,
            BackupEnabled = backupEnabled,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
        volume.Id = id;
        return volume;
    }

    /// <summary>
    /// Applies a partial update, re-validating the resulting state. The volume
    /// <see cref="Type"/> is immutable once created.
    /// </summary>
    internal void Apply(
        Optional<string> name,
        Optional<string> source,
        Optional<string> target,
        Optional<bool> readOnly,
        Optional<bool> backupEnabled)
    {
        var newName = name.HasValue ? name.Value.Trim() : Name;
        var newTarget = target.HasValue ? target.Value.Trim() : Target;
        var newSource = source.HasValue ? (string.IsNullOrWhiteSpace(source.Value) ? null : source.Value.Trim()) : Source;

        Validate(Type, newName, newTarget, newSource);

        Name = newName;
        Target = newTarget;
        Source = Type == VolumeType.Managed ? null : newSource;
        if (readOnly.HasValue) ReadOnly = readOnly.Value;
        if (backupEnabled.HasValue) BackupEnabled = backupEnabled.Value;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(VolumeType type, string name, string target, string? source)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Volume name is required.");

        if (name.Contains('/') || name.Contains('\\') || name is "." or "..")
            throw new ValidationException("Volume name cannot contain path separators or be a relative path segment ('.' or '..').");

        if (string.IsNullOrWhiteSpace(target))
            throw new ValidationException("Volume target (container path) is required.");

        if (!target.StartsWith('/'))
            throw new ValidationException("Volume target must be an absolute container path (starting with '/').");

        switch (type)
        {
            case VolumeType.HostPath:
                if (string.IsNullOrWhiteSpace(source))
                    throw new ValidationException("Host path volume requires a source host path.");
                if (!source.StartsWith('/'))
                    throw new ValidationException("Host path volume source must be an absolute host path (starting with '/').");
                break;

            case VolumeType.Named:
                if (string.IsNullOrWhiteSpace(source))
                    throw new ValidationException("Named volume requires a source volume name.");
                break;

            case VolumeType.Managed:
                // Source is derived from the service/volume ids at deploy time; nothing to validate.
                break;
        }
    }
}