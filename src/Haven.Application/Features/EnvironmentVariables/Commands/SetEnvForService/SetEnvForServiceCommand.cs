using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

public class SetEnvForServiceCommand : ICommand
{
    public Guid ServiceId { get; set; }
    public string EnvFile { get; set; }
}