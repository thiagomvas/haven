using Haven.Domain;

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
    
    Task CommitAsync(string localRepositoryPath, string commitMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes a new git repository at the specified path.
    /// </summary>
    Task InitRepositoryAsync(string localRepositoryPath, CancellationToken cancellationToken = default);
}