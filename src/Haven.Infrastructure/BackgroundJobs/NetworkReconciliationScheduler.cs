using Hangfire;

using Microsoft.Extensions.Hosting;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class NetworkReconciliationScheduler(IRecurringJobManager recurringJobManager) : IHostedService
{
    private const string JobId = "network-reconciliation";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        recurringJobManager.AddOrUpdate<NetworkReconciliationJob>(
            JobId,
            job => job.ExecuteAsync(),
            Cron.Hourly());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
