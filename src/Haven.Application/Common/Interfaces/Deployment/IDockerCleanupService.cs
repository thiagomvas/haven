namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Sweeps for Haven-managed Docker containers and images that are no longer referenced by any
/// Service, and removes those older than the given grace period.
/// </summary>
public interface IDockerCleanupService
{
    Task<DockerCleanupResult> CleanupOrphanedResourcesAsync(TimeSpan gracePeriod, bool dryRun, CancellationToken cancellationToken);
}

public sealed record DockerCleanupResult(
    IReadOnlyList<string> RemovedContainerIds,
    IReadOnlyList<string> RemovedImageTags);
