using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Projects.Commands.UpdateProject;

[RequirePermission(Permissions.ProjectManagement.Create)]
public sealed class UpdateProjectCommand : ICommand<Guid>
{
    public Guid Id { get; set; }
    public Optional<string> Name { get; set; }
    public Optional<string?> Description { get; set; }
}