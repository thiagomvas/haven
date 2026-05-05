using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

public class SetEnvForProjectCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public string EnvFile { get; set; }
}