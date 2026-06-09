using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Services.Commands.RestartService;

[RequirePermission(Permissions.ProjectManagement.ManageDeploys)]
public class RestartServiceCommand : ICommand
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}