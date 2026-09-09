using Hangfire;

using Microsoft.Extensions.Hosting;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HavenVersionCheckScheduler(
    IRecurringJobManager recurringJobManager,
    IBackgroundJobClient backgroundJobClient) : IHostedService
{
    private const string JobId = "haven-version-check";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobManager.AddOrUpdate<HavenVersionCheckJob>(
            JobId,
            job => job.ExecuteAsync(),
            Cron.Daily());

        backgroundJobClient.Enqueue<HavenVersionCheckJob>(job => job.ExecuteAsync());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}