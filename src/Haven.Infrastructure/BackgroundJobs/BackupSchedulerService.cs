using Hangfire;

using Haven.Application.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class BackupSchedulerService(
    IRecurringJobManager recurringJobManager,
    IOptionsMonitor<BackupOptions> backupOptions,
    ILogger<BackupSchedulerService> logger)
    : IHostedService, IDisposable
{
    private const string JobId = "automated-backup";
    private IDisposable? _optionsChangeListener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        ApplySchedule(backupOptions.CurrentValue);

        _optionsChangeListener = backupOptions.OnChange(ApplySchedule);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _optionsChangeListener?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _optionsChangeListener?.Dispose();

    private void ApplySchedule(BackupOptions options)
    {
        if (!options.Enabled)
        {
            recurringJobManager.RemoveIfExists(JobId);
            logger.LogInformation("Automated backup disabled — recurring job removed");
            return;
        }

        recurringJobManager.AddOrUpdate<BackupBackgroundJob>(
            JobId,
            job => job.ExecuteAsync(),
            options.CronExpression);

        logger.LogInformation(
            "Automated backup scheduled with cron '{Cron}'", options.CronExpression);
    }
}
