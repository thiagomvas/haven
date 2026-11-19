using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Application.Features.Services;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Dashboard.Queries.GetDashboardOverview;

public sealed class GetDashboardOverviewHandler(IProjectRepository repository, IServiceStateReader stateReader)
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
        var crashed = 0;
        var degraded = 0;
        var deploymentPending = 0;
        var deploying = 0;
        var unknown = 0;

        AttentionEnvironmentDto? attention = null;
        var attentionSeverity = int.MaxValue;

        LastDeploymentDto? lastDeployment = null;
        var deploymentsLast24h = 0;
        var since = DateTime.UtcNow.AddHours(-24);

        // Materialize first: the streaming reader would otherwise stay open while the state reader queries on the same connection.
        var projects = await repository.GetAsync(cancellationToken).ToListAsync(cancellationToken);

        foreach (var project in projects)
        {
            totalProjects++;
            totalEnvironments += project.Environments.Count;

            var projectServices = project.Environments.SelectMany(e => e.Services).ToList();
            var state = await stateReader.LoadAsync(projectServices.Select(x => x.Id), cancellationToken);
            var projectStats = ServiceStatisticsCalculator.Compute(projectServices, state);
            total += projectStats.Total;
            running += projectStats.Running;
            stopped += projectStats.Stopped;
            crashed += projectStats.Crashed;
            degraded += projectStats.Degraded;
            deploymentPending += projectStats.DeploymentPending;
            deploying += projectStats.Deploying;
            unknown += projectStats.Unknown;

            foreach (var environment in project.Environments)
            {
                if (environment.Services.Count == 0)
                    continue;

                var displayStatuses = environment.Services.Select(s => state.GetDisplayStatus(s.Id)).ToList();
                var runningCount = displayStatuses.Count(x => x is ServiceDisplayStatus.Running or ServiceDisplayStatus.Degraded);
                var affectedServiceCount = environment.Services.Count(service =>
                {
                    var status = state.GetDisplayStatus(service.Id);
                    return status is ServiceDisplayStatus.Degraded or ServiceDisplayStatus.Crashed
                        || state.HasFailedDeployment(service.Id);
                });

                if (affectedServiceCount > 0)
                {
                    var status = runningCount == 0 ? HealthStatus.Stopped : HealthStatus.Degraded;
                    if (AttentionSeverity.TryGetValue(status, out var severity) && severity < attentionSeverity)
                    {
                        attentionSeverity = severity;
                        attention = new AttentionEnvironmentDto
                        {
                            ProjectId = project.Id,
                            ProjectName = project.Name,
                            EnvironmentId = environment.Id,
                            EnvironmentName = environment.Name,
                            Status = status,
                            AffectedServiceCount = affectedServiceCount,
                        };
                    }
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
                Crashed = crashed,
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