using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Commands.DeleteProject;

[RequirePermission(Permissions.ProjectManagement.Delete)]
public sealed class DeleteProjectCommand : ICommand
{
    public Guid Id { get; set; }
}
