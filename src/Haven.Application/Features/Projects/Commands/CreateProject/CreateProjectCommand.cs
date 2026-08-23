using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Commands.CreateProject;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class CreateProjectCommand : ICommand<Guid>, IMutatesManifestState
{
    public string Name { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public string? Description { get; set; }
}