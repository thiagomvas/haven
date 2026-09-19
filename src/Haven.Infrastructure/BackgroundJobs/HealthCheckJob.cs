using Haven.Application.Common.Interfaces.Services;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class HealthCheckJob(IHealthCheckExecutor executor)
{
    public async Task ExecuteAsync(Guid healthCheckId, CancellationToken cancellationToken = default) =>
        await executor.ExecuteAsync(healthCheckId, cancellationToken);
}
