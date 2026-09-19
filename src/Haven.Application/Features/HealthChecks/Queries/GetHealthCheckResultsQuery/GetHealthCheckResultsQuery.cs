using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.HealthChecks.Queries.GetHealthCheckResultsQuery;

[RequirePermission(Permissions.ProjectManagement.ManageConfig)]
public sealed class GetHealthCheckResultsQuery : IQuery<IReadOnlyList<HealthCheckResultDto>>
{
    public Guid HealthCheckId { get; set; }
    public int Limit { get; set; } = 50;
}
