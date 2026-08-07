using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Deployment;

public interface IGitProvider
{
    GitProviderType Type { get; }

    /// <summary>
    /// Clones a git repository to the specified local path.
    /// </summary>
    Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pulls the latest changes from a remote branch in an existing local repository.
    /// </summary>
    Task PullAsync(string localRepositoryPath, string branch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available branches from a remote repository.
    /// </summary>
    Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default);

    Task CommitAsync(string localRepositoryPath, string commitMessage, string branch = "main", CancellationToken cancellationToken = default);

    /// <summary>
    /// Pushes the local branch to a remote repository.
    /// </summary>
    Task PushAsync(string localRepositoryPath, string remoteUrl, string branch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a new git repository at the specified path.
    /// </summary>
    Task InitRepositoryAsync(string localRepositoryPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent commits from a local repository.
    /// </summary>
    Task<IReadOnlyList<GitCommitInfo>> GetCommitsAsync(string localRepositoryPath, int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns repositories the authenticated credential owns or has access to.
    /// </summary>
    Task<IReadOnlyList<GitRepositorySummary>> GetAccessibleRepositoriesAsync(CancellationToken cancellationToken = default);
}