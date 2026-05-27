namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class ProjectDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public List<EnvironmentDashboardDto> Environments { get; set; } = [];
    public int TotalServices { get; set; }
    public int TotalServicesRunning { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public int TotalEnvVars { get; set; }
}
