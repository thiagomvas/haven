using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class EnvironmentDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Alias { get; set; }
    public string? Description { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public string NetworkName { get; set; } = default!;
    public ServiceStatisticsDto ServiceStatistics { get; set; } = default!;
    public HealthStatus Status { get; set; }
    public int TotalEnvVars { get; set; }
    public List<EnvironmentVariableDto> EnvironmentVariables { get; set; } = [];
    public List<ServiceDashboardDto> Services { get; set; } = [];
    public Dictionary<string, ServiceStatus> ServiceStatusMap { get; set; } = [];
}