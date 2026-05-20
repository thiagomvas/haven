using Haven.Application.Common;
using Haven.Domain.Entities;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IGitService
{
    /// <summary>
    /// Clones a git repository for a service into its designated directory.
    /// Returns the local repository path on success.
    /// Storage: {root}/services/{serviceId}
    /// </summary>
    Task<Result<string>> CloneServiceRepositoryAsync(
        Guid serviceId,
        string repositoryUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls the latest changes from a remote branch for a service's repository.
    /// </summary>
    Task<Result> PullServiceRepositoryAsync(
        Guid serviceId,
        string branch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available branches from a remote repository.
    /// Optionally authenticates using the provided git credentials.
    /// Does not require a local clone to exist.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> GetRemoteBranchesAsync(
        string repositoryUrl,
        GitCredentials? credentials = null,
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
    /// Deletes the local repository for a service.
    /// </summary>
    Task DeleteServiceRepositoryAsync(
        Guid serviceId,
        CancellationToken cancellationToken = default);
}
