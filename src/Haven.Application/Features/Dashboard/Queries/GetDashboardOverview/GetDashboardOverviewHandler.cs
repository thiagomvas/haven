using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;

public sealed class GetDashboardOverviewHandler(IProjectRepository repository)
    : IQueryHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private static readonly Dictionary<HealthStatus, int> AttentionSeverity = new()
    {
        [HealthStatus.Degraded] = 0,
        [HealthStatus.Stopped] = 1,
    };

    public async ValueTask<Result<DashboardOverviewDto>> Handle(GetDashboardOverviewQuery query, CancellationToken cancellationToken)
    {
        var totalProjects = 0;
        var totalEnvironments = 0;
        var total = 0;
        var running = 0;
        var stopped = 0;
        var degraded = 0;
        var deploymentPending = 0;
        var deploying = 0;
        var unknown = 0;

        AttentionEnvironmentDto? attention = null;
        var attentionSeverity = int.MaxValue;

        LastDeploymentDto? lastDeployment = null;
        var deploymentsLast24h = 0;
        var since = DateTime.UtcNow.AddHours(-24);

        await foreach (var project in repository.GetAsync(cancellationToken))
        {
            totalProjects++;
            totalEnvironments += project.Environments.Count;

            var (projTotal, projRunning, projStopped, projDegraded, projDeploymentPending, projDeploying, projUnknown) =
                project.GetServiceStatistics();
            total += projTotal;
            running += projRunning;
            stopped += projStopped;
            degraded += projDegraded;
            deploymentPending += projDeploymentPending;
            deploying += projDeploying;
            unknown += projUnknown;

            foreach (var environment in project.Environments)
            {
                if (environment.Services.Count == 0)
                    continue;

                var runningCount = environment.Services.Count(s => s.Status == ServiceStatus.Running);
                var affectedServiceCount = environment.Services.Count(
                    s => s.Status != ServiceStatus.Running || s.Health != ServiceHealth.Healthy);

                HealthStatus? status = affectedServiceCount == 0
                    ? null
                    : runningCount == 0
                        ? HealthStatus.Stopped
                        : HealthStatus.Degraded;

                if (status is { } envStatus
                    && AttentionSeverity.TryGetValue(envStatus, out var severity)
                    && severity < attentionSeverity)
                {
                    attentionSeverity = severity;
                    attention = new AttentionEnvironmentDto
                    {
                        ProjectId = project.Id,
                        ProjectName = project.Name,
                        EnvironmentId = environment.Id,
                        EnvironmentName = environment.Name,
                        Status = envStatus,
                        AffectedServiceCount = affectedServiceCount,
                    };
                }

                foreach (var service in environment.Services)
                {
                    if (service.LastDeployedAt is not { } deployedAt)
                        continue;

                    if (deployedAt >= since)
                        deploymentsLast24h++;

                    if (lastDeployment is null || deployedAt > lastDeployment.DeployedAt)
                    {
                        lastDeployment = new LastDeploymentDto
                        {
                            ServiceName = service.Name,
                            ProjectName = project.Name,
                            EnvironmentName = environment.Name,
                            DeployedAt = deployedAt,
                        };
                    }
                }
            }
        }

        return new DashboardOverviewDto
        {
            TotalProjects = totalProjects,
            TotalEnvironments = totalEnvironments,
            ServiceStatistics = new ServiceStatisticsDto
            {
                Total = total,
                Running = running,
                Stopped = stopped,
                Degraded = degraded,
                DeploymentPending = deploymentPending,
                Deploying = deploying,
                Unknown = unknown,
            },
            AttentionEnvironment = attention,
            DeploymentsLast24h = deploymentsLast24h,
            LastDeployment = lastDeployment,
        };
    }
}