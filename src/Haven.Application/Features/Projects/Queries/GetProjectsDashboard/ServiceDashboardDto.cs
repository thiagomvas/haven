using Haven.Domain;

namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class ServiceDashboardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public ServiceStatus Status { get; set; }
}
