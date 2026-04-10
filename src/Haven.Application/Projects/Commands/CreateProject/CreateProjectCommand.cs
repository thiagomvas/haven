using Haven.Application.Common.Messaging;

namespace Haven.Application.Projects.Commands.CreateProject;

public sealed class CreateProjectCommand : ICommand<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}