namespace Haven.Application.Common.Interfaces.Deployment;

using Haven.Domain;
using Haven.Domain.Entities;

/// <summary>
/// Factory for creating IGitProvider instances based on provider type and optional credentials.
/// </summary>
public interface IGitProviderFactory
{
    /// <summary>
    /// Creates a git provider for the specified type with optional credentials.
    /// </summary>
    IGitProvider Create(GitProviderType type, GitCredentials? credentials = null);
}
