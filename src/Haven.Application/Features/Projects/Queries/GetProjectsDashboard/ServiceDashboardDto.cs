using Haven.Domain;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class ServiceDashboardDto
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    public string Name { get; set; } = default!;
    public ServiceType Type { get; set; }
    public ExposureMode ExposureMode { get; set; }
    public ServiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastDeployedAt { get; set; }
    public ServiceSourceConfig? SourceConfig { get; set; }
}
