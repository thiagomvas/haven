using Hangfire;

using Haven.Application.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DockerCleanupSchedulerService(
    IRecurringJobManager recurringJobManager,
    IOptionsMonitor<DockerCleanupOptions> dockerCleanupOptions,
    ILogger<DockerCleanupSchedulerService> logger)
    : IHostedService, IDisposable
{
    private const string JobId = "docker-cleanup";
    private IDisposable? _optionsChangeListener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplySchedule(dockerCleanupOptions.CurrentValue);

        _optionsChangeListener = dockerCleanupOptions.OnChange(ApplySchedule);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _optionsChangeListener?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _optionsChangeListener?.Dispose();

    private void ApplySchedule(DockerCleanupOptions options)
    {
        if (!options.Enabled)
        {
            recurringJobManager.RemoveIfExists(JobId);
            logger.LogInformation("Docker cleanup disabled — recurring job removed");
            return;
        }

        recurringJobManager.AddOrUpdate<DockerCleanupJob>(
            JobId,
            job => job.ExecuteAsync(),
            options.CronExpression);

        logger.LogInformation("Docker cleanup scheduled with cron '{Cron}'", options.CronExpression);
    }
}
