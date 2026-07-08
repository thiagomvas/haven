using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeleteVolumeFile;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class DeleteVolumeFileCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public Guid VolumeId { get; set; }
    public string Path { get; set; } = string.Empty;
}