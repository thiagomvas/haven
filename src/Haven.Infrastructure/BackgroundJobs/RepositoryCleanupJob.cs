using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Haven.Infrastructure.BackgroundJobs;

public sealed class RepositoryCleanupJob(
    IGitRepositoryPathProvider pathProvider,
    IGitService gitService,
    IServiceRepository serviceRepository,
    IOptionsMonitor<RepositoryCleanupOptions> options,
    ILogger<RepositoryCleanupJob> logger)
{
    public async Task ExecuteAsync()
    {
        var current = options.CurrentValue;
        logger.LogInformation(
            "Running repository cleanup (grace period: {GracePeriodHours}h, dry run: {DryRun})",
            current.GracePeriodHours, current.DryRun);

        var servicesRoot = Path.Combine(pathProvider.GetRepositoryRootPath(), "services");
        if (!Directory.Exists(servicesRoot))
        {
            logger.LogInformation("No services directory found at '{Path}', nothing to clean up", servicesRoot);
            return;
        }

        var directoriesById = new Dictionary<Guid, string>();
        foreach (var dir in Directory.GetDirectories(servicesRoot))
        {
            if (Guid.TryParse(Path.GetFileName(dir), out var serviceId))
                directoriesById[serviceId] = dir;
        }

        if (directoriesById.Count == 0)
        {
            logger.LogInformation("No repository directories to evaluate");
            return;
        }

        var missingIds = await serviceRepository.FilterMissingIdsAsync(
            directoriesById.Keys.ToList(), CancellationToken.None);

        var cutoff = DateTime.UtcNow - TimeSpan.FromHours(current.GracePeriodHours);
        var deletedCount = 0;

        foreach (var missingId in missingIds)
        {
            var dir = directoriesById[missingId];
            var lastWriteUtc = Directory.GetLastWriteTimeUtc(dir);

            if (lastWriteUtc > cutoff)
            {
                logger.LogInformation(
                    "Skipping dangling repository '{ServiceId}': within grace period (last write {LastWrite})",
                    missingId, lastWriteUtc);
                continue;
            }

            if (current.DryRun)
            {
                logger.LogInformation("[DryRun] Would delete dangling repository directory '{Path}'", dir);
                continue;
            }

            await gitService.DeleteServiceRepositoryAsync(missingId, CancellationToken.None);
            deletedCount++;
        }

        logger.LogInformation("Repository cleanup completed: {Count} dangling repository(ies) removed", deletedCount);
    }
}