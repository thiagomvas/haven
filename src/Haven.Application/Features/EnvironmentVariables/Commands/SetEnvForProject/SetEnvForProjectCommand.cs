using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForProject;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class SetEnvForProjectCommand : ICommand, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public string EnvFile { get; set; }
}