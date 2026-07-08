using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Services.Commands.AddVolume;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class AddVolumeCommand : ICommand<Guid>
{
    public Guid ServiceId { get; set; }
    public VolumeType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool ReadOnly { get; set; }
    public bool BackupEnabled { get; set; }
}