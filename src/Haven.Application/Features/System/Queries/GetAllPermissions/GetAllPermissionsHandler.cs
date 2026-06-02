using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetAllPermissions;

public sealed class GetAllPermissionsHandler : IQueryHandler<GetAllPermissionsQuery, string[]>
{
    public ValueTask<Result<string[]>> Handle(GetAllPermissionsQuery query, CancellationToken cancellationToken)
        => ValueTask.FromResult(Result<string[]>.Success(Permissions.All.ToArray()));
}
