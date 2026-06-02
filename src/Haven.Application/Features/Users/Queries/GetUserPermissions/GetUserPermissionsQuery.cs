using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Queries.GetUserPermissions;

[RequirePermission(Permissions.Users.View)]
public sealed class GetUserPermissionsQuery : IQuery<string[]>
{
    public Guid UserId { get; init; }
}
