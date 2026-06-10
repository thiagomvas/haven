using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public abstract class GitProviderBase(GitCredentials? credentials, IEncryptionService encryptionService, ILogger<GitProviderBase> logger) : IGitProvider
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

    public Task CommitAsync(string localRepositoryPath, string commitMessage, CancellationToken cancellationToken = default)
    {
        using var repo = new Repository(localRepositoryPath);
        Commands.Stage(repo, "*");

        if (!repo.RetrieveStatus().IsDirty)
            return Task.CompletedTask;

        var author = CreateSignature();

        repo.Commit(commitMessage, author, author);

        logger.LogInformation("Committed changes to {Repository}: {Message}", localRepositoryPath, commitMessage);

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
                    Username = credentials.Username,
                    Password = encryptionService.Decrypt(credentials.PrimaryCredential)
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
                    Username = credentials.Username,
                    Password = encryptionService.Decrypt(credentials.PrimaryCredential)
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
                    Username = credentials.Username,
                    Password = encryptionService.Decrypt(credentials.PrimaryCredential)
                };
        }

        return options;
    }
    
    protected Signature CreateSignature()
    {
        var authorName = credentials?.Username ?? "Haven";
        var authorEmail = credentials?.Username ?? "haven@localhost";
        return new Signature(authorName, authorEmail, DateTimeOffset.UtcNow);
    }
}