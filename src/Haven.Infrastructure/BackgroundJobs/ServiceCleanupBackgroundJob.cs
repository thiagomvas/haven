using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.BackgroundJobs;

/// <summary>
/// Tears down a service's deployment (stopping/removing its container, and for Dockerfile
/// services, its built image) after the service row itself has already been deleted by a
/// restore/sync. Runs against a detached in-memory snapshot rather than a repository lookup,
/// since there is no row left to load.
/// </summary>
public sealed class ServiceCleanupBackgroundJob(
    IDeployServiceFactory deployServiceFactory,
    ILogger<ServiceCleanupBackgroundJob> logger)
{
    public async Task ExecuteAsync(ServiceCleanupInfo info)
    {
        var service = Service.CreateDetachedSnapshotForCleanup(
            info.ServiceId, info.ServiceName, info.ServiceAlias, info.Type, info.SourceConfigJson);

        var deployService = deployServiceFactory.Create(service);
        if (deployService is null)
        {
            logger.LogDebug(
                "No deploy service found for removed service '{ServiceName}', skipping cleanup",
                info.ServiceName);
            return;
        }

        await deployService.CleanupAsync(service, CancellationToken.None);

        logger.LogInformation(
            "Cleaned up deployment for removed service '{ServiceName}' ({ServiceId})",
            info.ServiceName, info.ServiceId);
    }
}