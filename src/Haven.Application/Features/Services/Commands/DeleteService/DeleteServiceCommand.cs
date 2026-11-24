using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeleteService;

[RequirePermission(Permissions.ProjectManagement.Delete)]
public sealed class DeleteServiceCommand : ICommand, IMutatesManifestState
{
    public Guid ServiceId { get; set; }
}