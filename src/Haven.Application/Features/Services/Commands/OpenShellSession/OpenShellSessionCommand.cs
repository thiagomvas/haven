using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Services.Commands.OpenShellSession;

[RequirePermission(Permissions.ProjectManagement.ManageShell)]
public sealed class OpenShellSessionCommand : ICommand<IShellSession>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
    public ShellType ShellType { get; set; }
}
