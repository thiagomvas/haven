using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HealthCheckJob(
    IHealthCheckRepository healthCheckRepository,
    IServiceRepository serviceRepository,
    IHealthCheckRunnerFactory runnerFactory,
    IUnitOfWork unitOfWork,
    ILogger<HealthCheckJob> logger)
{
    public async Task ExecuteAsync(Guid healthCheckId, CancellationToken cancellationToken = default)
    {
        var healthCheck = await healthCheckRepository.GetByIdAsync(healthCheckId, cancellationToken);
        if (healthCheck is null)
        {
            logger.LogInformation("Health check '{HealthCheckId}' no longer exists, skipping run", healthCheckId);
            return;
        }

        var service = await serviceRepository.GetByIdAsync(healthCheck.ServiceId, cancellationToken);
        if (service is null)
        {
            logger.LogWarning("Service '{ServiceId}' not found for health check '{HealthCheckId}'", healthCheck.ServiceId, healthCheckId);
            return;
        }

        var runner = runnerFactory.Create(healthCheck.Kind);
        var result = await runner.RunHealthCheckAsync(healthCheck, cancellationToken);

        service.RecordHealthCheckResult(healthCheck, result);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Health check '{HealthCheckId}' ran with result {Result}", healthCheckId, result);
    }
}