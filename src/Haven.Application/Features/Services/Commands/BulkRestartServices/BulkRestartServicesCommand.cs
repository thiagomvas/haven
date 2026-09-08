using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Services.Shared;

namespace Haven.Application.Features.Services.Commands.BulkRestartServices;

[RequirePermission(Permissions.ProjectManagement.ManageDeploys)]
public sealed class BulkRestartServicesCommand : ICommand<BulkServiceActionResponse>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public IReadOnlyList<Guid> ServiceIds { get; set; } = [];
}