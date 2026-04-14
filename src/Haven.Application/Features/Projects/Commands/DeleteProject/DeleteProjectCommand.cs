using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommand : ICommand
{
    public Guid Id { get; set; }
}
