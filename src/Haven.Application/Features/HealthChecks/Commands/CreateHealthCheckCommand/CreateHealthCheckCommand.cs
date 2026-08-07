using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.HealthChecks.Commands.CreateHealthCheckCommand;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class CreateHealthCheckCommand : ICommand<Guid>
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public HealthCheckKind Kind { get; set; }
    public bool Enabled { get; set; } = true;
    public string? CronExpression { get; set; }
    public string Config { get; set; } = string.Empty;
}