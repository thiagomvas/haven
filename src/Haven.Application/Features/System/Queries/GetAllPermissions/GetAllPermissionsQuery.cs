using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetAllPermissions;

[AuthenticatedOnly]
public sealed class GetAllPermissionsQuery : IQuery<string[]>
{
    public bool ShowOnlyAttributedPermissions { get; set; }
}