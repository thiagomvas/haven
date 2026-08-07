using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GitProviderFactory(
    ILoggerFactory factory,
    IGitHubOAuthService oauthService,
    IUnitOfWork unitOfWork,
    IMemoryCache cache) : IGitProviderFactory
{
    public IGitProvider Create(GitProviderType type, GitCredentials? credentials = null)
    {
        return type switch
        {
            GitProviderType.GitHub => new GitHubGitProvider(credentials, factory.CreateLogger<GitHubGitProvider>(), oauthService, unitOfWork, cache),
            GitProviderType.Generic => new GenericGitProvider(credentials, factory.CreateLogger<GenericGitProvider>()),
            _ => new GenericGitProvider(credentials, factory.CreateLogger<GenericGitProvider>()),
        };
    }
}