using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Queries.GetUserPermissions;

[RequirePermission(Permissions.System.ManageUsers)]
public sealed class GetUserPermissionsQuery : IQuery<string[]>
{
    public Guid UserId { get; init; }
}
