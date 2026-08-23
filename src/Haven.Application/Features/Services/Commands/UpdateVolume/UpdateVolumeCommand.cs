using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.UpdateVolume;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UpdateVolumeCommand : ICommand, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
    public Guid VolumeId { get; set; }
    public string? Name { get; set; }
    public string? Source { get; set; }
    public string? Target { get; set; }
    public bool? ReadOnly { get; set; }
    public bool? BackupEnabled { get; set; }
}