using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.CustomActions.Commands.DeleteCustomAction;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class DeleteCustomActionCommand : ICommand, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
    public Guid ActionId { get; set; }
}
