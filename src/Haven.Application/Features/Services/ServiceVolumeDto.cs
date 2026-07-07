using Haven.Domain;

namespace Haven.Application.Features.Services;

public sealed class ServiceVolumeDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public VolumeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string Target { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public bool BackupEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
