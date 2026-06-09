using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Commands.SetUserPermissions;

[AdminOnly]
public sealed class SetUserPermissionsCommand : ICommand
{
    public Guid UserId { get; set; }
    public string[] Permissions { get; set; } = [];
}