using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HealthChecks.Commands.UpdateHealthCheckCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class UpdateHealthCheckCommand : ICommand
{
    public Guid HealthCheckId { get; set; }
    public string? Name { get; set; }
    public bool? Enabled { get; set; }
    public string? CronExpression { get; set; }
    public bool ClearCronExpression { get; set; }
    public string? Config { get; set; }
}