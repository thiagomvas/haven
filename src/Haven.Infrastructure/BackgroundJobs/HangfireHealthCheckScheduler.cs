using Hangfire;

using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Entities;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HangfireHealthCheckScheduler(
    IRecurringJobManager recurringJobManager,
    IBackgroundJobClient backgroundJobClient) : IHealthCheckScheduler
{
    private static string JobId(Guid healthCheckId) => $"health-check-{healthCheckId}";

    public void Schedule(HealthCheck healthCheck)
    {
        var jobId = JobId(healthCheck.Id);

        if (!healthCheck.Enabled || string.IsNullOrWhiteSpace(healthCheck.CronExpression))
        {
            recurringJobManager.RemoveIfExists(jobId);
            return;
        }

        recurringJobManager.AddOrUpdate<HealthCheckJob>(
            jobId,
            job => job.ExecuteAsync(healthCheck.Id, CancellationToken.None),
            healthCheck.CronExpression);
    }

    public void Unschedule(Guid healthCheckId) => recurringJobManager.RemoveIfExists(JobId(healthCheckId));

    public void RunNow(Guid healthCheckId) =>
        backgroundJobClient.Enqueue<HealthCheckJob>(job => job.ExecuteAsync(healthCheckId, CancellationToken.None));
}