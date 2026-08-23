using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Commands.DisableSidecar;

[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class DisableSidecarCommand : ICommand, IMutatesManifestState
{
    public Guid SidecarId { get; set; }
}