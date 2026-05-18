using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public abstract class GitProviderBase(GitCredentials credentials, IEncryptionService encryptionService, ILogger<GitProviderBase> logger) : IGitProvider
{
    public abstract GitProviderType Type { get; }
    public abstract Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default);

    public abstract Task PullAsync(string repositoryUrl, string branch, CancellationToken cancellationToken = default);

    public abstract Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl,
        CancellationToken cancellationToken = default);
    
    protected CloneOptions CreateCloneOptions(GitCredentials credentials)
    {
        var options = new CloneOptions();
        if (credentials.AuthMethod is GitAuthMethod.Token)
        {
            options.FetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
                new UsernamePasswordCredentials()
                {
                    Username = credentials.Username,
                    Password = encryptionService.Decrypt(credentials.PrimaryCredential)
                };
        }

        return options;
    }
}