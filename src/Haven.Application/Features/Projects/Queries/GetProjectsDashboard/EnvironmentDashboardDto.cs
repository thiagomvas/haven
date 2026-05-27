using Haven.Domain;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class EnvironmentDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string NetworkName { get; set; } = default!;
    public ServiceStatisticsDto ServiceStatistics { get; set; } = default!;
    public HealthStatus Status { get; set; }
    public int TotalEnvVars { get; set; }
    public List<EnvironmentVariableDto> EnvironmentVariables { get; set; } = [];
}