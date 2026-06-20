using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserHandler(IUserRepository userRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteUserCommand>
{
    public async ValueTask<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId == command.Id)
            return Error.Failure("Users.CannotDeleteSelf", "You cannot delete your own account.");

        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null)
            return Error.NotFoundFor(nameof(User), command.Id);

        userRepository.Remove(user);
        return Result.Success();
    }
}