using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Users.Commands.ResendInvite;

[RequirePermission(Permissions.System.ManageUsers)]
public sealed class ResendInviteCommand : ICommand
{
    public Guid UserId { get; set; }
}
