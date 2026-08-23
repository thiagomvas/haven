using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Commands.CloneProject;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CloneProjectCommand : ICommand<Guid>, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public string NewName { get; set; } = string.Empty;
    public string? NewAlias { get; set; }
}