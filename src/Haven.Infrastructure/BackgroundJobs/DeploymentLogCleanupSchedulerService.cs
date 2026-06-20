using Hangfire;

using Haven.Application.Configuration;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DeploymentLogCleanupSchedulerService(
    IRecurringJobManager recurringJobManager,
    IOptionsMonitor<InstanceOptions> instanceOptions,
    ILogger<DeploymentLogCleanupSchedulerService> logger)
    : IHostedService, IDisposable
{
    private const string JobId = "deployment-log-cleanup";
    private IDisposable? _optionsChangeListener;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RegisterJob();
        _optionsChangeListener = instanceOptions.OnChange(_ => RegisterJob());
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _optionsChangeListener?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => _optionsChangeListener?.Dispose();

    private void RegisterJob()
    {
        recurringJobManager.AddOrUpdate<DeploymentLogCleanupJob>(
            JobId,
            job => job.ExecuteAsync(),
            Cron.Daily());

        logger.LogInformation("Deployment log cleanup job scheduled (daily)");
    }
}