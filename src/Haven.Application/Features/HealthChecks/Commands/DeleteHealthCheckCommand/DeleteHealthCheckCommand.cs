using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HealthChecks.Commands.DeleteHealthCheckCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class DeleteHealthCheckCommand : ICommand
{
    public Guid HealthCheckId { get; set; }
}