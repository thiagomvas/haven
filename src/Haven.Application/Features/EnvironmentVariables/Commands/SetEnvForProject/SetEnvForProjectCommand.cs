using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

[RequirePermission(Permissions.Projects.Update)]
public class SetEnvForProjectCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public string EnvFile { get; set; }
}