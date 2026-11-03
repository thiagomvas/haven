using Haven.Application.Common;
using Haven.Application.Common.Messaging;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;

[RequirePermission(Permissions.ProjectManagement.Read)]
public sealed class GetDashboardAttentionQuery : IQuery<List<AttentionServiceDto>>
{
    /// <summary>Maximum number of services returned. Defaults to 10.</summary>
    public int Limit { get; init; } = 10;
}
