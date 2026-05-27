using Haven.Domain;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class EnvironmentDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string NetworkName { get; set; } = default!;
    public int TotalServices { get; set; }
    public int ServicesRunning { get; set; }
    public HealthStatus Status { get; set; }
}