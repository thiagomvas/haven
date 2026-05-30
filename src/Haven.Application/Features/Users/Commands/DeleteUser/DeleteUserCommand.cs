using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Commands.DeleteUser;

[RequirePermission(Permissions.Users.Delete)]
public sealed class DeleteUserCommand : ICommand
{
    public Guid Id { get; set; }
}
