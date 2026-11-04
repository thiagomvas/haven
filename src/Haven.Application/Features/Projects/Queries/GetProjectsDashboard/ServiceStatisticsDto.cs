namespace Haven.Application.Features.Projects.Queries.GetProjectsDashboard;

public sealed class ServiceStatisticsDto
{
    public int Total { get; set; }
    public int Running { get; set; }
    /// <summary>Stopped includes Crashed, to keep the original contract; Crashed is the subset that stopped abnormally.</summary>
    public int Stopped { get; set; }
    public int Crashed { get; set; }
    public int Degraded { get; set; }
    public int DeploymentPending { get; set; }
    public int Deploying { get; set; }
    public int Unknown { get; set; }
}