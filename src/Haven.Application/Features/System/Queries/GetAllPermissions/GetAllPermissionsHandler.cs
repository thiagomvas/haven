using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetAllPermissions;

public sealed class GetAllPermissionsHandler(ICurrentUserService currentUserService, IPermissionRepository repository) : IQueryHandler<GetAllPermissionsQuery, string[]>
{
    public async ValueTask<Result<string[]>> Handle(GetAllPermissionsQuery query, CancellationToken cancellationToken)
    {
        if (query.ShowOnlyAttributedPermissions && !currentUserService.IsAdmin)
        {
            var userId = currentUserService.UserId;
            if (userId is null) return Error.Forbidden;
            var permissions = await repository.GetUserPermissionsAsync(userId.Value, cancellationToken);
            return permissions.Select(p => p.Name).ToArray();
        }

        return Result<string[]>.Success([.. Permissions.All]);
    }
}