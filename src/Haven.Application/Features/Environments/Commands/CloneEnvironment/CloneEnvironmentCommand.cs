using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Commands.CloneEnvironment;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CloneEnvironmentCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string NewName { get; set; } = string.Empty;
    public string? NewAlias { get; set; }
    public Guid? TargetProjectId { get; set; }
}