using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.System.Queries.GetAllPermissions;

[RequirePermission(Permissions.Users.ManagePermissions)]
public sealed class GetAllPermissionsQuery : IQuery<string[]>;
