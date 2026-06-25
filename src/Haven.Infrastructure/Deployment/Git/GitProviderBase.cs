using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public abstract class GitProviderBase(GitCredentials? credentials, ILogger<GitProviderBase> logger) : IGitProvider
{
    public abstract GitProviderType Type { get; }
    public abstract Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default);

    public abstract Task PullAsync(string repositoryUrl, string branch, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl,
        CancellationToken cancellationToken = default);

    public Task InitRepositoryAsync(string localRepositoryPath, CancellationToken cancellationToken = default)
    {
        Repository.Init(localRepositoryPath);
        logger.LogInformation("Initialized git repository at {Path}", localRepositoryPath);
        return Task.CompletedTask;
    }

    public Task CommitAsync(string localRepositoryPath, string commitMessage, string branch = "main", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(branch))
            branch = "main";

        using var repo = new Repository(localRepositoryPath);
        Commands.Stage(repo, "*");

        if (!repo.Commits.Any())
        {
            if (!repo.RetrieveStatus().IsDirty)
                return Task.CompletedTask; // Fresh repo with nothing to commit — branch cannot be created yet

            // Point HEAD at the desired branch before the first commit creates it
            repo.Refs.UpdateTarget("HEAD", $"refs/heads/{branch}");
        }
        else
        {
            if (repo.Head.FriendlyName != branch)
            {
                var localBranch = repo.Branches[branch] ?? repo.CreateBranch(branch);
                Commands.Checkout(repo, localBranch);
            }

            if (!repo.RetrieveStatus().IsDirty)
                return Task.CompletedTask; // Nothing new to commit, but branch already exists for push
        }

        var author = CreateSignature();
        repo.Commit(commitMessage, author, author);

        logger.LogInformation("Committed changes to {Repository} on {Branch}: {Message}", localRepositoryPath, branch, commitMessage);

        return Task.CompletedTask;
    }

    public Task PushAsync(string localRepositoryPath, string remoteUrl, string branch, CancellationToken cancellationToken = default)
    {
        using var repo = new Repository(localRepositoryPath);

        if (repo.Branches[branch] is null)
        {
            logger.LogWarning("Local branch {Branch} does not exist, skipping push", branch);
            return Task.CompletedTask;
        }

        var remote = repo.Network.Remotes["origin"];
        if (remote is null)
            remote = repo.Network.Remotes.Add("origin", remoteUrl);
        else if (remote.Url != remoteUrl)
            repo.Network.Remotes.Update("origin", r => r.Url = remoteUrl);

        var pushOptions = CreatePushOptions();
        repo.Network.Push(remote, $"refs/heads/{branch}:refs/heads/{branch}", pushOptions);

        logger.LogInformation("Pushed branch {Branch} to {RemoteUrl}", branch, remoteUrl);

        return Task.CompletedTask;
    }

    protected CloneOptions CreateCloneOptions(GitCredentials? credentials)
    {
        var options = new CloneOptions();

        if (credentials?.AuthMethod is GitAuthMethod.Token)
        {
            options.FetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = credentials.Username ?? "token",
                    Password = credentials.PrimaryCredential.Value
                };
        }

        options.FetchOptions.Depth = 1;

        return options;
    }

    protected PullOptions CreatePullOptions(GitCredentials? credentials)
    {
        var options = new PullOptions();

        if (options.FetchOptions == null)
        {
            options.FetchOptions = new FetchOptions();
        }

        if (credentials?.AuthMethod is GitAuthMethod.Token)
        {
            options.FetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = credentials.Username ?? "token",
                    Password = credentials.PrimaryCredential.Value
                };
        }

        options.FetchOptions.Depth = 1;
        return options;
    }

    protected ProxyOptions CreateProxyOptions(GitCredentials? credentials)
    {
        var options = new ProxyOptions();

        if (credentials?.AuthMethod is GitAuthMethod.Token)
        {
            options.CredentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = credentials.Username ?? "token",
                    Password = credentials.PrimaryCredential.Value
                };
        }

        return options;
    }

    protected PushOptions CreatePushOptions()
    {
        var options = new PushOptions();

        if (credentials?.AuthMethod is GitAuthMethod.Token)
        {
            options.CredentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials
                {
                    Username = credentials.Username ?? "token",
                    Password = credentials.PrimaryCredential.Value
                };
        }

        return options;
    }

    public Task<IReadOnlyList<GitCommitInfo>> GetCommitsAsync(string localRepositoryPath, int limit = 50, CancellationToken cancellationToken = default)
    {
        var repoPath = Repository.Discover(localRepositoryPath);
        if (string.IsNullOrEmpty(repoPath))
            return Task.FromResult<IReadOnlyList<GitCommitInfo>>([]);

        using var repo = new Repository(repoPath);
        var commits = repo.Commits
            .Take(limit)
            .Select(c => new GitCommitInfo(c.Sha, c.MessageShort, c.Author.Name, c.Author.When))
            .ToList();

        return Task.FromResult<IReadOnlyList<GitCommitInfo>>(commits);
    }

    protected Signature CreateSignature()
    {
        var authorName = credentials?.Username ?? "Haven";
        var authorEmail = credentials?.Username ?? "haven@localhost";
        return new Signature(authorName, authorEmail, DateTimeOffset.UtcNow);
    }
}