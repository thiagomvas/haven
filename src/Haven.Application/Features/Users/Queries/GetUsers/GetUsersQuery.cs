using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Queries.GetUsers;

[RequirePermission(Permissions.System.ManageUsers)]
public sealed class GetUsersQuery : IQuery<List<UserDto>> { }
