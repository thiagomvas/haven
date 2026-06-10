using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GenericGitProvider(GitCredentials? credentials, ILogger<GenericGitProvider> logger) : GitProviderBase(credentials, logger)
{
    public override GitProviderType Type => GitProviderType.Generic;

    public override async Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = CreateCloneOptions(credentials);
            Repository.Clone(repositoryUrl, destinationPath, options);
            logger.LogInformation("Repository cloned from {Url} to {Path}", repositoryUrl, destinationPath);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clone repository from {Url} to {Path}", repositoryUrl, destinationPath);
            throw;
        }
    }

    public override async Task PullAsync(string localRepositoryPath, string branch, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = CreatePullOptions(credentials);
            var repoPath = Repository.Discover(localRepositoryPath);
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new InvalidOperationException($"No Git repository found at path: {localRepositoryPath}");

            using var repo = new Repository(repoPath);

            logger.LogInformation("Fetching latest changes from remote for repository at {Path}", localRepositoryPath);

            // Fetch from origin
            try
            {
                Commands.Fetch(repo, "origin", new string[] { }, options.FetchOptions, null);
            }
            catch (LibGit2SharpException ex) when (ex.Message.Contains("no remote"))
            {
                logger.LogWarning("No origin remote found, skipping fetch");
            }

            // Check out the branch and reset to remote state
            var remoteBranch = repo.Branches.FirstOrDefault(b => b.IsRemote && b.FriendlyName == $"origin/{branch}");
            if (remoteBranch != null)
            {
                var localBranch = repo.Branches[branch];
                if (localBranch == null)
                {
                    localBranch = repo.CreateBranch(branch, remoteBranch.Tip);
                }

                logger.LogInformation("Checking out branch {Branch} in repository at {Path}", branch, localRepositoryPath);
                Commands.Checkout(repo, localBranch);

                logger.LogInformation("Resetting branch {Branch} to remote state", branch);
                repo.Reset(ResetMode.Hard, remoteBranch.Tip);
            }
            else
            {
                logger.LogWarning("Remote branch origin/{Branch} not found, checking out local branch if it exists", branch);
                var localBranch = repo.Branches[branch];
                if (localBranch != null)
                {
                    Commands.Checkout(repo, localBranch);
                }
            }

            logger.LogInformation("Repository updated to latest remote state for branch {Branch}", branch);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pull changes for branch {Branch} from {Path}", branch, localRepositoryPath);
            throw;
        }
    }

    public override Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var opt = CreateProxyOptions(credentials);
            var entries = Repository.ListRemoteReferences(repositoryUrl, opt);
            var branches = entries.Select(e => e.CanonicalName)
                .Where(name => name.StartsWith("refs/heads/"))
                .Select(name => name.Substring("refs/heads/".Length))
                .ToList();

            logger.LogDebug("Retrieved {BranchCount} branches from repository {Url}", branches.Count, repositoryUrl);

            return Task.FromResult<IReadOnlyList<string>>(branches);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get branches from repository");
            throw;
        }

    }
}