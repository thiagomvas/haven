using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Sidecars.Queries.GetSidecarManifest;

[RequirePermission(Permissions.Sidecars.Read)]
public sealed class GetSidecarManifestQuery : IQuery<string>
{
    public Guid SidecarId { get; set; }
}