using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Domain.Aggregates;

namespace Haven.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserHandler(IUserRepository userRepository, IAuthService authService)
    : ICommandHandler<CreateUserCommand, UserDto>
{
    public async ValueTask<Result<UserDto>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(command.Email, cancellationToken))
            return Error.ConflictFor(nameof(User), command.Email);

        var result = await authService.CreateUserAsync(command.Name, command.Email, command.TemporaryPassword);
        if (result.IsFailure)
            return result.Error;

        var user = await userRepository.GetByIdAsync(result.Value, cancellationToken);
        return Result<UserDto>.CreatedFor(new UserDto(user!.Id, user.Name, user.Email, user.IsAdmin, user.RequirePasswordChange));
    }
}
