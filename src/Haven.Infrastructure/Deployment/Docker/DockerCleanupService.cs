using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment;

/// <inheritdoc cref="IDockerCleanupService" />
public sealed class DockerCleanupService(
    IDockerClient dockerClient,
    IServiceScopeFactory scopeFactory,
    ILogger<DockerCleanupService> logger)
    : IDockerCleanupService
{
    public async Task<DockerCleanupResult> CleanupOrphanedResourcesAsync(TimeSpan gracePeriod, bool dryRun, CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow - gracePeriod;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HavenDbContext>();

        var removedContainers = await CleanupOrphanedContainersAsync(db, cutoff, dryRun, cancellationToken);
        var removedImages = await CleanupUnusedImagesAsync(db, cutoff, dryRun, cancellationToken);

        return new DockerCleanupResult(removedContainers, removedImages);
    }

    private async Task<IReadOnlyList<string>> CleanupOrphanedContainersAsync(
        HavenDbContext db, DateTime cutoff, bool dryRun, CancellationToken cancellationToken)
    {
        var parameters = new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "label", new Dictionary<string, bool> { { "haven.managed=true", true } } }
            }
        };

        var containers = await dockerClient.Containers.ListContainersAsync(parameters, cancellationToken);
        var existingServiceIds = (await db.Services.Select(s => s.Id).ToListAsync(cancellationToken)).ToHashSet();

        var removed = new List<string>();

        foreach (var container in containers)
        {
            if (!container.Labels.TryGetValue("haven.service.id", out var serviceIdValue) ||
                !Guid.TryParse(serviceIdValue, out var serviceId) ||
                existingServiceIds.Contains(serviceId))
            {
                continue;
            }

            if (container.Created > cutoff)
                continue;

            if (dryRun)
            {
                logger.LogInformation(
                    "[DryRun] Would remove orphaned container '{ContainerId}' (no Service '{ServiceId}' found)",
                    container.ID, serviceId);
                removed.Add(container.ID);
                continue;
            }

            try
            {
                if (container.State == "running")
                    await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters(), cancellationToken);

                await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true }, cancellationToken);
                logger.LogInformation(
                    "Removed orphaned container '{ContainerId}' (no Service '{ServiceId}' found)", container.ID, serviceId);
                removed.Add(container.ID);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove orphaned container '{ContainerId}'", container.ID);
            }
        }

        return removed;
    }

    private async Task<IReadOnlyList<string>> CleanupUnusedImagesAsync(
        HavenDbContext db, DateTime cutoff, bool dryRun, CancellationToken cancellationToken)
    {
        var services = await db.Services
            .Include(s => s.Environment)
            .ThenInclude(e => e!.Project)
            .ToListAsync(cancellationToken);

        // Images are built/tagged without an explicit tag component (see DockerfileDeployService),
        // so Docker implicitly tags them "latest"; RepoTags always reports the full "repo:tag" form.
        var expectedTags = services
            .Select(s => $"{DockerUtils.BuildImageTag(s.Environment?.Project?.Alias, s.Environment?.Alias, s.Alias, s.Id)}:latest")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters { All = false }, cancellationToken);

        var removed = new List<string>();

        foreach (var image in images)
        {
            if (image.RepoTags is null)
                continue;

            var havenTags = image.RepoTags.Where(t => t.StartsWith("haven-", StringComparison.OrdinalIgnoreCase)).ToList();
            if (havenTags.Count == 0)
                continue;

            if (image.Created > cutoff)
                continue;

            foreach (var tag in havenTags)
            {
                if (expectedTags.Contains(tag))
                    continue;

                if (dryRun)
                {
                    logger.LogInformation("[DryRun] Would remove unused Haven image '{ImageTag}'", tag);
                    removed.Add(tag);
                    continue;
                }

                try
                {
                    await dockerClient.Images.DeleteImageAsync(tag, new ImageDeleteParameters { Force = true }, cancellationToken);
                    logger.LogInformation("Removed unused Haven image '{ImageTag}'", tag);
                    removed.Add(tag);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to remove unused Haven image '{ImageTag}'", tag);
                }
            }
        }

        return removed;
    }
}