using System.Runtime.InteropServices;

using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public abstract class GitProviderBase(GitCredentials? credentials, ILogger<GitProviderBase> logger) : IGitProvider
{
    public abstract GitProviderType Type { get; }
    public abstract Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default);

    public abstract Task PullAsync(string repositoryUrl, string branch, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl,
        CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<GitRepositorySummary>> GetAccessibleRepositoriesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hook for providers whose credentials can go stale (e.g. an OAuth access token nearing expiry).
    /// Called before any operation that authenticates against the remote, so a refreshed token is in
    /// place by the time <see cref="CreateCloneOptions"/>/<see cref="CreatePullOptions"/>/
    /// <see cref="CreateProxyOptions"/>/<see cref="CreatePushOptions"/> read it. No-op by default.
    /// </summary>
    protected virtual Task EnsureCredentialsFreshAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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

    public async Task PushAsync(string localRepositoryPath, string remoteUrl, string branch, CancellationToken cancellationToken = default)
    {
        await EnsureCredentialsFreshAsync(cancellationToken);

        using var repo = new Repository(localRepositoryPath);

        if (repo.Branches[branch] is null)
        {
            logger.LogWarning("Local branch {Branch} does not exist, skipping push", branch);
            return;
        }

        var remote = repo.Network.Remotes["origin"];
        if (remote is null)
            remote = repo.Network.Remotes.Add("origin", remoteUrl);
        else if (remote.Url != remoteUrl)
            repo.Network.Remotes.Update("origin", r => r.Url = remoteUrl);

        if (credentials?.AuthMethod is GitAuthMethod.Ssh)
        {
            var sshKeyPath = WriteTemporarySshKey(credentials);
            try
            {
                await GitCliRunner.RunAsync(
                    ["push", "origin", $"refs/heads/{branch}:refs/heads/{branch}"],
                    localRepositoryPath,
                    sshKeyPath,
                    cancellationToken);
            }
            finally
            {
                DeleteTemporarySshKey(sshKeyPath);
            }
        }
        else
        {
            var pushOptions = CreatePushOptions();
            repo.Network.Push(remote, $"refs/heads/{branch}:refs/heads/{branch}", pushOptions);
        }

        logger.LogInformation("Pushed branch {Branch} to {RemoteUrl}", branch, remoteUrl);
    }

    /// <summary>
    /// GitHub's smart-HTTP endpoint challenges for auth even on anonymous clones/fetches, and libgit2
    /// requires a credentials callback to be set to respond to that challenge — otherwise it fails with
    /// "remote authentication required but no callback set" even for public repositories.
    /// <see cref="DefaultCredentials"/> (NTLM/Negotiate) is not a usable fallback here: libgit2 builds
    /// without Windows Integrated Auth support (e.g. Linux containers) reject it with "could not find
    /// appropriate mechanism for credentials". So an empty <see cref="UsernamePasswordCredentials"/> is
    /// used as the anonymous fallback when no token/OAuth credentials are configured for the service.
    /// </summary>
    private static CredentialsHandler CreateCredentialsHandler(GitCredentials? credentials)
    {
        if (credentials?.AuthMethod is GitAuthMethod.Token or GitAuthMethod.OAuth)
        {
            return (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = credentials.Username ?? "token",
                    Password = credentials.PrimaryCredential.Value
                };
        }

        return (url, usernameFromUrl, types) =>
            new UsernamePasswordCredentials()
            {
                Username = string.Empty,
                Password = string.Empty
            };
    }

    protected CloneOptions CreateCloneOptions(GitCredentials? credentials)
    {
        var options = new CloneOptions();

        options.FetchOptions.CredentialsProvider = CreateCredentialsHandler(credentials);
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

        options.FetchOptions.CredentialsProvider = CreateCredentialsHandler(credentials);
        options.FetchOptions.Depth = 1;
        return options;
    }

    protected ProxyOptions CreateProxyOptions(GitCredentials? credentials)
    {
        var options = new ProxyOptions();

        options.CredentialsProvider = CreateCredentialsHandler(credentials);

        return options;
    }

    protected PushOptions CreatePushOptions()
    {
        var options = new PushOptions();

        options.CredentialsProvider = CreateCredentialsHandler(credentials);

        return options;
    }

    /// <summary>
    /// Writes the credentials' SSH private key to a restricted-permission temp file so it can be handed to
    /// the system `ssh` client via GIT_SSH_COMMAND. LibGit2Sharp's bundled native binaries have no SSH
    /// transport, so SSH operations shell out to the system `git`/`ssh` binaries instead.
    /// </summary>
    protected static string? WriteTemporarySshKey(GitCredentials? credentials)
    {
        if (credentials?.AuthMethod is not GitAuthMethod.Ssh)
            return null;

        var path = Path.Combine(Path.GetTempPath(), $"haven-ssh-{Guid.NewGuid():N}.key");
        File.WriteAllText(path, credentials.PrimaryCredential.Value);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        return path;
    }

    protected static void DeleteTemporarySshKey(string? path)
    {
        if (path is null)
            return;

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup; the temp file will be picked up by OS/temp-dir cleanup eventually.
        }
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