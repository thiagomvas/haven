using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Models;
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
        await EnsureCredentialsFreshAsync(cancellationToken);

        if (credentials?.AuthMethod is GitAuthMethod.Ssh)
        {
            var sshKeyPath = WriteTemporarySshKey(credentials);
            try
            {
                await GitCliRunner.RunAsync(
                    ["clone", "--depth", "1", repositoryUrl, destinationPath],
                    workingDirectory: null,
                    sshKeyPath,
                    cancellationToken);
                logger.LogInformation("Repository cloned via SSH from {Url} to {Path}", repositoryUrl, destinationPath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to clone repository via SSH from {Url} to {Path}", repositoryUrl, destinationPath);
                throw;
            }
            finally
            {
                DeleteTemporarySshKey(sshKeyPath);
            }
            return;
        }

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
        await EnsureCredentialsFreshAsync(cancellationToken);

        if (credentials?.AuthMethod is GitAuthMethod.Ssh)
        {
            await PullViaSshAsync(localRepositoryPath, branch, cancellationToken);
            return;
        }

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

    private async Task PullViaSshAsync(string localRepositoryPath, string branch, CancellationToken cancellationToken)
    {
        var sshKeyPath = WriteTemporarySshKey(credentials);
        try
        {
            logger.LogInformation("Fetching latest changes from remote for repository at {Path}", localRepositoryPath);
            await GitCliRunner.RunAsync(["fetch", "origin"], localRepositoryPath, sshKeyPath, cancellationToken);

            var hasRemoteBranch = true;
            try
            {
                await GitCliRunner.RunAsync(
                    ["show-ref", "--verify", "--quiet", $"refs/remotes/origin/{branch}"],
                    localRepositoryPath, sshKeyPath, cancellationToken);
            }
            catch (GitCliException)
            {
                hasRemoteBranch = false;
            }

            if (hasRemoteBranch)
            {
                logger.LogInformation("Checking out branch {Branch} in repository at {Path}", branch, localRepositoryPath);
                await GitCliRunner.RunAsync(
                    ["checkout", "-B", branch, $"origin/{branch}"],
                    localRepositoryPath, sshKeyPath, cancellationToken);
            }
            else
            {
                logger.LogWarning("Remote branch origin/{Branch} not found, checking out local branch if it exists", branch);
                try
                {
                    await GitCliRunner.RunAsync(["checkout", branch], localRepositoryPath, sshKeyPath, cancellationToken);
                }
                catch (GitCliException)
                {
                    // No local branch either; nothing to check out.
                }
            }

            logger.LogInformation("Repository updated to latest remote state for branch {Branch}", branch);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pull changes for branch {Branch} from {Path}", branch, localRepositoryPath);
            throw;
        }
        finally
        {
            DeleteTemporarySshKey(sshKeyPath);
        }
    }

    public override async Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        if (credentials?.AuthMethod is GitAuthMethod.Ssh)
        {
            var sshKeyPath = WriteTemporarySshKey(credentials);
            try
            {
                var output = await GitCliRunner.RunAsync(
                    ["ls-remote", "--heads", repositoryUrl],
                    workingDirectory: null,
                    sshKeyPath,
                    cancellationToken);

                var sshBranches = output
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Split('\t'))
                    .Where(parts => parts.Length == 2 && parts[1].StartsWith("refs/heads/"))
                    .Select(parts => parts[1]["refs/heads/".Length..])
                    .ToList();

                logger.LogDebug("Retrieved {BranchCount} branches from repository {Url}", sshBranches.Count, repositoryUrl);

                return sshBranches;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to get branches from repository");
                throw;
            }
            finally
            {
                DeleteTemporarySshKey(sshKeyPath);
            }
        }

        try
        {
            var opt = CreateProxyOptions(credentials);
            var entries = Repository.ListRemoteReferences(repositoryUrl, opt);
            var branches = entries.Select(e => e.CanonicalName)
                .Where(name => name.StartsWith("refs/heads/"))
                .Select(name => name.Substring("refs/heads/".Length))
                .ToList();

            logger.LogDebug("Retrieved {BranchCount} branches from repository {Url}", branches.Count, repositoryUrl);

            return branches;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get branches from repository");
            throw;
        }
    }

    public override Task<IReadOnlyList<GitRepositorySummary>> GetAccessibleRepositoriesAsync(CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Repository listing is not supported for generic git providers");
        return Task.FromResult<IReadOnlyList<GitRepositorySummary>>([]);
    }
}