using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.StopService;

public class StopServiceCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}