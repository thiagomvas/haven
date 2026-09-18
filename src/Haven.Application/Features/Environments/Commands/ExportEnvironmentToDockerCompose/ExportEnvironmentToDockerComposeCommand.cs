using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Commands.ExportEnvironmentToDockerCompose;

// Export includes feature flags as env vars, so ManageConfig is required alongside Read to avoid a permission bypass.
[RequirePermission(Permissions.ProjectManagement.Read)]
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class ExportEnvironmentToDockerComposeCommand : ICommand<string>
{
    public Guid EnvironmentId { get; set; }

    /// <summary>
    /// Ids of the services to include in the export. Services in the environment that aren't listed here
    /// are omitted, letting the caller export a subset instead of the entire environment.
    /// </summary>
    public List<Guid> ServiceIds { get; set; } = [];
}