using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForEnvironment;

[RequirePermission(Permissions.Environments.Update)]
public class SetEnvForEnvironmentCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public string EnvFile { get; set; }
}