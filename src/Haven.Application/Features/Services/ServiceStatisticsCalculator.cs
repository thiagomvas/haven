using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Features.Projects.Queries.GetProjectsDashboard;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

namespace Haven.Application.Features.Services;

/// <summary>
/// Rolls per-service display statuses up into the counts and environment status the dashboards show.
/// Buckets keep the original API contract: Running includes Degraded, NotDeployed counts as Stopped, Crashed counts as Stopped and is also tallied separately,
/// a queued deployment counts as DeploymentPending, and any other in-flight deployment or a restart counts as Deploying.
/// </summary>
public static class ServiceStatisticsCalculator
{
    public static ServiceStatisticsDto Compute(IEnumerable<Service> services, ServiceStateSnapshot snapshot)
    {
        var stats = new ServiceStatisticsDto();
        foreach (var service in services)
        {
            stats.Total++;
            switch (Classify(service.Id, snapshot))
            {
                case ServiceDisplayStatus.Running:
                    stats.Running++;
                    break;
                case ServiceDisplayStatus.Degraded:
                    stats.Running++;
                    stats.Degraded++;
                    break;
                case ServiceDisplayStatus.Queued:
                    stats.DeploymentPending++;
                    break;
                case ServiceDisplayStatus.Deploying:
                    stats.Deploying++;
                    break;
                case ServiceDisplayStatus.Starting:
                    stats.Deploying++;
                    break;
                case ServiceDisplayStatus.Unknown:
                    stats.Unknown++;
                    break;
                case ServiceDisplayStatus.Crashed:
                    stats.Stopped++;
                    stats.Crashed++;
                    break;
                default:
                    stats.Stopped++;
                    break;
            }
        }

        return stats;
    }

    public static ServiceStatisticsDto Sum(IEnumerable<ServiceStatisticsDto> parts)
    {
        var total = new ServiceStatisticsDto();
        foreach (var p in parts)
        {
            total.Total += p.Total;
            total.Running += p.Running;
            total.Stopped += p.Stopped;
            total.Crashed += p.Crashed;
            total.Degraded += p.Degraded;
            total.DeploymentPending += p.DeploymentPending;
            total.Deploying += p.Deploying;
            total.Unknown += p.Unknown;
        }

        return total;
    }

    public static HealthStatus GetEnvironmentStatus(ServiceStatisticsDto stats)
    {
        if (stats.Total == 0) return HealthStatus.Unknown;
        if (stats.Running == stats.Total) return HealthStatus.Healthy;
        if (stats.Running > 0) return HealthStatus.Degraded;
        if (stats.Deploying > 0) return HealthStatus.Deploying;
        if (stats.DeploymentPending > 0) return HealthStatus.DeploymentPending;
        return HealthStatus.Stopped;
    }

    private static ServiceDisplayStatus Classify(Guid serviceId, ServiceStateSnapshot snapshot) =>
        snapshot.GetDisplayStatus(serviceId);
}