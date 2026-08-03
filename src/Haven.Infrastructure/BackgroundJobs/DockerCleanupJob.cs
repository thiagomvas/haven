using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class DockerCleanupJob(
    IDockerCleanupService dockerCleanupService,
    IOptionsMonitor<DockerCleanupOptions> options,
    ILogger<DockerCleanupJob> logger)
{
    public async Task ExecuteAsync()
    {
        var current = options.CurrentValue;
        logger.LogInformation(
            "Running Docker cleanup (grace period: {GracePeriodHours}h, dry run: {DryRun})",
            current.GracePeriodHours, current.DryRun);

        var result = await dockerCleanupService.CleanupOrphanedResourcesAsync(
            TimeSpan.FromHours(current.GracePeriodHours), current.DryRun, CancellationToken.None);

        logger.LogInformation(
            "Docker cleanup completed: {ContainerCount} container(s), {ImageCount} image(s) removed",
            result.RemovedContainerIds.Count, result.RemovedImageTags.Count);
    }
}