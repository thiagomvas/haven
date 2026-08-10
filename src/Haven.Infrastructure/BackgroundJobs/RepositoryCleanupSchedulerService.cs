using Hangfire;

using Haven.Application.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class RepositoryCleanupSchedulerService(
    IRecurringJobManager recurringJobManager,
    IOptionsMonitor<RepositoryCleanupOptions> repositoryCleanupOptions,
    ILogger<RepositoryCleanupSchedulerService> logger)
    : IHostedService, IDisposable
{
    private const string JobId = "repository-cleanup";
    private IDisposable? _optionsChangeListener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplySchedule(repositoryCleanupOptions.CurrentValue);

        _optionsChangeListener = repositoryCleanupOptions.OnChange(ApplySchedule);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _optionsChangeListener?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _optionsChangeListener?.Dispose();

    private void ApplySchedule(RepositoryCleanupOptions options)
    {
        if (!options.Enabled)
        {
            recurringJobManager.RemoveIfExists(JobId);
            logger.LogInformation("Repository cleanup disabled: recurring job removed");
            return;
        }

        recurringJobManager.AddOrUpdate<RepositoryCleanupJob>(
            JobId,
            job => job.ExecuteAsync(),
            options.CronExpression);

        logger.LogInformation("Repository cleanup scheduled with cron '{Cron}'", options.CronExpression);
    }
}
