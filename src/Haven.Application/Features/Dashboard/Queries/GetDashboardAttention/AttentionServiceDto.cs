namespace Haven.Application.Features.Dashboard.Queries.GetDashboardAttention;

/// <summary>Why a service is on the dashboard's "needs attention" list.</summary>
public enum AttentionReason
{
    /// <summary>The container is running but its health check is failing.</summary>
    Unhealthy,

    /// <summary>The container exited unexpectedly.</summary>
    Crashed,

    /// <summary>The most recent deployment failed.</summary>
    DeploymentFailed,
}

public sealed class AttentionServiceDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = default!;
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public Guid EnvironmentId { get; set; }
    public string EnvironmentName { get; set; } = default!;
    public AttentionReason Reason { get; set; }

    /// <summary>
    /// When the problem started, when known: the crash time for crashed services, the deploy time for failed
    /// deployments. Unhealthy services carry no timestamp because health changes are not recorded.
    /// </summary>
    public DateTime? Since { get; set; }
}