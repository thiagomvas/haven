using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeleteVolume;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class DeleteVolumeCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public Guid VolumeId { get; set; }
}