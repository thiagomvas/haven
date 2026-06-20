using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Queries.GetUsers;

[RequirePermission(Permissions.System.ReadUsers)]
public sealed class GetUsersHandler(IUserRepository userRepository)
    : IQueryHandler<GetUsersQuery, List<UserDto>>
{
    public async ValueTask<Result<List<UserDto>>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return users.Select(u => new UserDto(u.Id, u.Name, u.Email, u.IsAdmin, u.RequirePasswordChange)).ToList();
    }
}