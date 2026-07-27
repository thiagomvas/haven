using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HealthChecks.Commands.RunHealthCheckNowCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class RunHealthCheckNowCommand : ICommand
{
    public Guid HealthCheckId { get; set; }
}
