using Haven.Application.Common.Interfaces.Deployment.Results;

namespace Haven.Application.Common.Interfaces.Deployment;

/// <summary>
/// Orchestrates git operations for services.
/// Provides a higher-level abstraction over IGitProvider with service-aware path management.
/// </summary>
public interface IGitService
{
    /// <summary>
    /// Clones a git repository for a service into its designated directory.
    /// The repository is shared across all environments.
    /// Storage: {root}/services/{serviceId}
    /// </summary>
    Task<GitCloneResult> CloneServiceRepositoryAsync(
        Guid serviceId,
        string repositoryUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls the latest changes from a remote branch for a service's repository.
    /// The repository is shared; different environments can check out different branches.
    /// </summary>
    Task<GitPullResult> PullServiceRepositoryAsync(
        Guid serviceId,
        string branch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available branches from a remote repository.
    /// Does not require a local clone to exist.
    /// </summary>
    Task<GitBranchesResult> GetRemoteBranchesAsync(
        string repositoryUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the local repository path for a service if it exists.
    /// </summary>
    string? GetServiceRepositoryPath(Guid serviceId);

    /// <summary>
    /// Checks if a service's repository has been cloned locally.
    /// </summary>
    bool ServiceRepositoryExists(Guid serviceId);

    /// <summary>
    /// Deletes the local repository for a service (affects all environments using this service).
    /// </summary>
    Task DeleteServiceRepositoryAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default);
}
