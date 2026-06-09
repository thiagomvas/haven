using Haven.Domain;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class ProjectDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Alias { get; set; }
    public string? Description { get; set; }
    public List<EnvironmentDashboardDto> Environments { get; set; } = [];
    public ServiceStatisticsDto ServiceStatistics { get; set; } = default!;
    public DateTime? LastDeployedAt { get; set; }
    public int TotalEnvVars { get; set; }
    public List<EnvironmentVariableDto> EnvironmentVariables { get; set; } = [];
    public Dictionary<string, ServiceStatus> ServiceStatusMap { get; set; } = [];
}