using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GitService(
    IGitRepositoryPathProvider repositoryPathProvider,
    IGitCredentialsRepository credentialsRepository,
    IGitProviderFactory gitProviderFactory,
    ILogger<GitService> logger) : IGitService
{
    public async Task<Result<string>> CloneServiceRepositoryAsync(Guid serviceId, string repositoryUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var destinationPath = repositoryPathProvider.GetServiceRepositoryPath(serviceId);

            // Ensure parent directory exists but NOT the destination itself
            // LibGit2Sharp.Repository.Clone expects the destination directory to not exist
            var parentPath = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            // Remove destination if it exists and is empty (from failed previous attempts)
            if (Directory.Exists(destinationPath) && !Directory.EnumerateFileSystemEntries(destinationPath).Any())
            {
                Directory.Delete(destinationPath);
            }

            var credentials = await credentialsRepository.GetByServiceIdAsync(serviceId, cancellationToken);
            var provider = gitProviderFactory.Create(GitProviderType.Generic, credentials);
            await provider.CloneRepositoryAsync(repositoryUrl, destinationPath, cancellationToken);

            logger.LogInformation("Repository cloned for service '{ServiceId}' to path '{Path}'", serviceId, destinationPath);
            return destinationPath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clone repository for service '{ServiceId}'", serviceId);
            return Error.Failed;
        }
    }

    public async Task<Result> PullServiceRepositoryAsync(Guid serviceId, string branch, CancellationToken cancellationToken = default)
    {
        try
        {
            var repositoryPath = repositoryPathProvider.GetServiceRepositoryPath(serviceId);

            if (!repositoryPathProvider.RepositoryDirectoryExists(serviceId))
            {
                logger.LogWarning("Repository not found for service '{ServiceId}' at path '{Path}'", serviceId, repositoryPath);
                return Error.NotFoundFor("Repository", serviceId);
            }

            var credentials = await credentialsRepository.GetByServiceIdAsync(serviceId, cancellationToken);
            var provider = gitProviderFactory.Create(GitProviderType.Generic, credentials);
            await provider.PullAsync(repositoryPath, branch, cancellationToken);

            logger.LogInformation("Repository pulled for service '{ServiceId}' branch '{Branch}'", serviceId, branch);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to pull repository for service '{ServiceId}'", serviceId);
            return Error.Failed;
        }
    }

    public async Task<Result<IReadOnlyList<string>>> GetRemoteBranchesAsync(string repositoryUrl, GitCredentials? credentials = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = gitProviderFactory.Create(credentials?.ProviderType ?? GitProviderType.Generic, credentials);
            var branches = await provider.GetBranchesAsync(repositoryUrl, cancellationToken);

            return Result<IReadOnlyList<string>>.Success(branches);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get remote branches for repository '{RepositoryUrl}'", repositoryUrl);
            return Error.Failed;
        }
    }

    public async Task<Result<IReadOnlyList<GitRepositorySummary>>> GetAccessibleRepositoriesAsync(GitCredentials credentials, CancellationToken cancellationToken = default)
    {
        try
        {
            var provider = gitProviderFactory.Create(credentials.ProviderType, credentials);
            var repositories = await provider.GetAccessibleRepositoriesAsync(cancellationToken);

            return Result<IReadOnlyList<GitRepositorySummary>>.Success(repositories);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get accessible repositories for credential '{CredentialId}'", credentials.Id);
            return Error.Failed;
        }
    }

    public string? GetServiceRepositoryPath(Guid serviceId)
    {
        if (!repositoryPathProvider.RepositoryDirectoryExists(serviceId))
            return null;

        return repositoryPathProvider.GetServiceRepositoryPath(serviceId);
    }

    public bool ServiceRepositoryExists(Guid serviceId)
    {
        return repositoryPathProvider.RepositoryDirectoryExists(serviceId);
    }

    public async Task DeleteServiceRepositoryAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var repositoryPath = repositoryPathProvider.GetServiceRepositoryPath(serviceId);

            if (Directory.Exists(repositoryPath))
            {
                Directory.Delete(repositoryPath, recursive: true);
                logger.LogInformation("Repository deleted for service '{ServiceId}'", serviceId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete repository for service '{ServiceId}'", serviceId);
        }
    }
}