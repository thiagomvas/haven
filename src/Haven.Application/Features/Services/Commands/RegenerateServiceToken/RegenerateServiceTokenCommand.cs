using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.RegenerateServiceToken;

public sealed class RegenerateServiceTokenCommand : ICommand<string>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}
