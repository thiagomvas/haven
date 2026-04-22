using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.DeployService;

public sealed class DeployServiceCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}
