using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

/// <summary>
/// Rebuilds every HealthCheck's Hangfire recurring job on startup, reconciling drift between the
/// database and Hangfire's recurring-job storage (e.g. after a manifest restore or manual DB edit).
/// </summary>
public sealed class HealthCheckSchedulerStartupService(
    IServiceScopeFactory scopeFactory,
    ILogger<HealthCheckSchedulerStartupService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var healthCheckRepository = scope.ServiceProvider.GetRequiredService<IHealthCheckRepository>();
        var scheduler = scope.ServiceProvider.GetRequiredService<IHealthCheckScheduler>();

        var healthChecks = await healthCheckRepository.GetAllAsync(cancellationToken);
        foreach (var healthCheck in healthChecks)
            scheduler.Schedule(healthCheck);

        logger.LogInformation("Reconciled {Count} health check recurring jobs on startup", healthChecks.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
