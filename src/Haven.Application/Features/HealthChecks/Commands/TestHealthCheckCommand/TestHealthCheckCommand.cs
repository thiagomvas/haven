using Haven.Application.Common;
using Haven.Application.Common.Messaging;
using Haven.Domain.Enums;

namespace Haven.Application.Features.HealthChecks.Commands.TestHealthCheckCommand;

/// <summary>Runs an unsaved health check configuration once against a service and returns the outcome without persisting anything.</summary>
[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class TestHealthCheckCommand : ICommand<HealthCheckResultDto>
{
    public Guid ServiceId { get; set; }
    public HealthCheckKind Kind { get; set; }
    public string Config { get; set; } = string.Empty;
}
