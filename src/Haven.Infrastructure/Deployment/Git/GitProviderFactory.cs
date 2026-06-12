using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GitProviderFactory(ILoggerFactory factory) : IGitProviderFactory
{
    public IGitProvider Create(GitProviderType type, GitCredentials? credentials = null)
    {
        return type switch
        {
            GitProviderType.Generic => new GenericGitProvider(credentials, factory.CreateLogger<GenericGitProvider>()),
            _ => new GenericGitProvider(credentials, factory.CreateLogger<GenericGitProvider>()),
        };
    }
}