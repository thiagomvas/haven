using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Environments.Commands.DeleteEnvironment;

public sealed class DeleteEnvironmentCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
}
