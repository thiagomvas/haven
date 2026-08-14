using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;

public sealed class DashboardOverviewDto
{
    public int TotalProjects { get; set; }
    public int TotalEnvironments { get; set; }
    public ServiceStatisticsDto ServiceStatistics { get; set; } = default!;
    public AttentionEnvironmentDto? AttentionEnvironment { get; set; }
    public int DeploymentsLast24h { get; set; }
    public LastDeploymentDto? LastDeployment { get; set; }
}

public sealed class AttentionEnvironmentDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = default!;
    public HealthStatus Status { get; set; }
    public int AffectedServiceCount { get; set; }
}

public sealed class LastDeploymentDto
{
    public string ServiceName { get; set; } = default!;
    public string ProjectName { get; set; } = default!;
    public string EnvironmentName { get; set; } = default!;
    public DateTime DeployedAt { get; set; }
}
