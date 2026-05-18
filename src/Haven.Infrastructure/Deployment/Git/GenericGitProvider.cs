using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GenericGitProvider(GitCredentials credentials, IEncryptionService encryptionService, ILogger<GenericGitProvider> logger) : GitProviderBase(credentials, encryptionService, logger)
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

    public override Task PullAsync(string localRepositoryPath, string branch, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}