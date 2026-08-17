using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Commands.ExportSidecarManifest;

[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class ExportSidecarManifestCommand : ICommand<string>
{
    public Guid SidecarId { get; set; }
}
