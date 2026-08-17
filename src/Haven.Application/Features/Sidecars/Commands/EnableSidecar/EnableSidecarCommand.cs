using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Commands.EnableSidecar;

[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class EnableSidecarCommand : ICommand
{
    public Guid SidecarId { get; set; }
}