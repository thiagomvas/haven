using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HealthChecks.Queries.GetServiceHealthChecksQuery;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public class GetServiceHealthChecksQuery : IQuery<IReadOnlyList<HealthCheckDto>>
{
    public Guid ProjectId { get; set; }
    public Guid EnvironmentId { get; set; }
    public Guid ServiceId { get; set; }
}
