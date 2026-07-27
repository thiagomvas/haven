using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Services;

public interface IHealthCheckScheduler
{
    /// <summary>Adds/updates the recurring job for this health check if it's enabled and has a cron expression, otherwise removes it.</summary>
    void Schedule(HealthCheck healthCheck);

    /// <summary>Removes the recurring job for this health check id, if any.</summary>
    void Unschedule(Guid healthCheckId);

    /// <summary>Enqueues a one-off run of this health check, independent of its recurring schedule.</summary>
    void RunNow(Guid healthCheckId);
}
