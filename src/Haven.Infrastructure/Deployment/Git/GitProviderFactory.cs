using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Logging;

namespace Haven.Infrastructure.Deployment.Git;

public class GitProviderFactory(IEncryptionService encryptionService, ILoggerFactory factory) : IGitProviderFactory
{
    public IGitProvider Create(GitProviderType type, GitCredentials? credentials = null)
    {
        return type switch
        {
            GitProviderType.Generic => new GenericGitProvider(credentials, encryptionService, factory.CreateLogger<GenericGitProvider>()),
            _ => throw new NotSupportedException($"Git provider type '{type}' is not supported.")
        };
    }
}