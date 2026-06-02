using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Users.Queries.GetUserPermissions;

public sealed class GetUserPermissionsHandler(IUserRepository userRepository)
    : IQueryHandler<GetUserPermissionsQuery, string[]>
{
    public async ValueTask<Result<string[]>> Handle(GetUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken);

        if (user is null)
            return Error.NotFoundFor(nameof(User), query.UserId);

        return user.Permissions.Select(p => p.Name).ToArray();
    }
}
