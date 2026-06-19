using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DeploymentLogCleanupJob(
    IDeploymentRepository deploymentRepository,
    IUnitOfWork unitOfWork,
    IOptionsMonitor<InstanceOptions> instanceOptions,
    ILogger<DeploymentLogCleanupJob> logger)
{
    public async Task ExecuteAsync()
    {
        var retentionCount = instanceOptions.CurrentValue.DeploymentLogRetentionCount;
        logger.LogInformation("Running deployment log cleanup (retention count: {Count})", retentionCount);

        var excess = await deploymentRepository.GetExcessDeploymentsAsync(retentionCount, CancellationToken.None);

        if (excess.Count == 0)
        {
            logger.LogInformation("No excess deployment logs to clean up");
            return;
        }

        foreach (var deployment in excess)
        {
            if (!string.IsNullOrEmpty(deployment.LogFile) && File.Exists(deployment.LogFile))
                File.Delete(deployment.LogFile);

            await deploymentRepository.RemoveAsync(deployment.Id, CancellationToken.None);
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        logger.LogInformation("Deleted {Count} excess deployment log(s)", excess.Count);
    }
}
