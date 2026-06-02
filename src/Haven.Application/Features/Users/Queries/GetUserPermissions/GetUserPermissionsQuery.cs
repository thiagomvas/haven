using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Queries.GetUserPermissions;

public sealed class GetUserPermissionsQuery : IQuery<string[]>
{
    public Guid UserId { get; init; }
}
