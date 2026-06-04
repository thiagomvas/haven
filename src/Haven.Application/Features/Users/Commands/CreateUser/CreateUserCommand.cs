using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Commands.CreateUser;

[RequirePermission(Common.Permissions.System.ManageUsers)]
public sealed class CreateUserCommand : ICommand<UserDto>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public bool IsAdmin { get; set; } = false;
    public string[] Permissions { get; set; } = [];
}
