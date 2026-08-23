using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.EnvironmentVariables.Commands.SetEnvForService;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class SetEnvForServiceCommand : ICommand, IMutatesManifestState
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
    public string EnvFile { get; set; }
}