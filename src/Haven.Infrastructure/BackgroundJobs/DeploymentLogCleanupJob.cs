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

        var basePath = instanceOptions.CurrentValue.DeploymentLogBasePath;
        var logFiles = Directory.Exists(basePath)
            ? Directory.GetFiles(basePath).ToList()
            : [];

        var filesByDeploymentId = new Dictionary<Guid, string>();
        foreach (var file in logFiles)
        {
            var idPart = Path.GetFileNameWithoutExtension(file).Split('_').FirstOrDefault();
            if (Guid.TryParse(idPart, out var deploymentId))
                filesByDeploymentId[deploymentId] = file;
        }

        var missingIds = await deploymentRepository.FilterMissingIdsAsync(filesByDeploymentId.Keys.ToList(), CancellationToken.None);

        foreach (var missingId in missingIds)
        {
            if (filesByDeploymentId.TryGetValue(missingId, out var fileToDelete) && File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
                logger.LogInformation("Deleted orphaned deployment log file: {File}", fileToDelete);
            }
        }
    }
}