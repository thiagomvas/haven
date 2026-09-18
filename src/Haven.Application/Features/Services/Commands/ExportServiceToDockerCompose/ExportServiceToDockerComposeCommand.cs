using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.ExportServiceToDockerCompose;

// Export includes feature flags as env vars, so ManageConfig is required alongside Read to avoid a permission bypass.
[RequirePermission(Permissions.ProjectManagement.Read)]
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class ExportServiceToDockerComposeCommand : ICommand<string>
{
    public Guid ServiceId { get; set; }
}