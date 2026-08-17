using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Commands.ImportSidecarManifest;

[RequirePermission(Permissions.Sidecars.Manage)]
public sealed class ImportSidecarManifestCommand : ICommand
{
    public Guid SidecarId { get; set; }

    /// <summary>
    /// Optional YAML manifest content (pasted or uploaded by the caller). When provided, this is
    /// applied directly instead of reading the sidecar's manifest file from disk.
    /// </summary>
    public string? ManifestYaml { get; set; }
}
