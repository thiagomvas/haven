using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Users.Commands.SetUserPermissions;

public sealed class SetUserPermissionsHandler(IUserRepository userRepository)
    : ICommandHandler<SetUserPermissionsCommand>
{
    public async ValueTask<Result> Handle(SetUserPermissionsCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null)
            return Error.NotFoundFor(nameof(User), command.UserId);

        if (user.IsAdmin)
            return Error.Failure("Users.AdminPermissions", "Cannot set permissions for admin users.");

        user.SetPermissions(command.Permissions);
        return Result.Success();
    }
}