using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public class GitCredentials : Entity
{
    public GitProviderType ProviderType { get; private set; } = GitProviderType.Generic;
    public string? HostUrl { get; private set; }
    public GitAuthMethod AuthMethod { get; private set; } = GitAuthMethod.Token;
    public string? Username { get; private set; }
    public EncryptedValue PrimaryCredential { get; private set; }
    public EncryptedValue? SecondaryCredential { get; private set; }
    public EncryptedValue? WebhookSecret { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset LastValidatedAt { get; private set; }

    private GitCredentials()
    {
    }

    public static GitCredentials Create(GitProviderType providerType, string? hostUrl, GitAuthMethod authMethod,
        EncryptedValue primaryCredential, EncryptedValue? secondaryCredential, string? webhookSecret,
        string displayName)
    {
        return new GitCredentials
        {
            ProviderType = providerType,
            HostUrl = hostUrl,
            AuthMethod = authMethod,
            PrimaryCredential = primaryCredential,
            SecondaryCredential = secondaryCredential,
            WebhookSecret = webhookSecret != null ? EncryptedValue.From(webhookSecret) : null,
            DisplayName = displayName,
            IsActive = true,
            LastValidatedAt = DateTimeOffset.UtcNow
        };
    }

    public static GitCredentials CreateFromToken(GitProviderType providerType, string? hostUrl, string token,
        string? webhookSecret, string displayName)
    {
        return new GitCredentials
        {
            ProviderType = providerType,
            HostUrl = hostUrl,
            AuthMethod = GitAuthMethod.Token,
            PrimaryCredential = EncryptedValue.From(token),
            WebhookSecret = webhookSecret != null ? EncryptedValue.From(webhookSecret) : null,
            DisplayName = displayName,
            IsActive = true,
            LastValidatedAt = DateTimeOffset.UtcNow
        };
    }
}