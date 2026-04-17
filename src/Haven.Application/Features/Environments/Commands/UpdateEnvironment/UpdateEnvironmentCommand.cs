using Haven.Application.Common.Messaging;
using Haven.Domain;

namespace Haven.Application.Features.Environments.Commands.UpdateEnvironment;

public sealed class UpdateEnvironmentCommand : ICommand<Guid>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Optional<string> Name { get; set; }
    public Optional<string?> Description { get; set; }
}
