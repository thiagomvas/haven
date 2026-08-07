using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class GitCredentialsTests
{
    [Test]
    public void MarkValidated_UpdatesLastValidatedAt()
    {
        var credentials = GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds");
        var before = DateTimeOffset.UtcNow;

        credentials.MarkValidated();

        credentials.LastValidatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void UpdateOAuthTokens_UpdatesLastValidatedAt()
    {
        var credentials = GitCredentials.CreateFromOAuth(GitProviderType.GitHub, null, "access-token",
            "refresh-token", DateTimeOffset.UtcNow.AddHours(1), "Test Creds");
        var before = DateTimeOffset.UtcNow;

        credentials.UpdateOAuthTokens("new-access-token", "new-refresh-token", DateTimeOffset.UtcNow.AddHours(1));

        credentials.LastValidatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }

    [Test]
    public void RotateManualCredential_UpdatesLastValidatedAt()
    {
        var credentials = GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds");
        var before = DateTimeOffset.UtcNow;

        credentials.RotateManualCredential(GitAuthMethod.Ssh, EncryptedValue.From("ssh-key"), null, null);

        credentials.LastValidatedAt.ShouldBeGreaterThanOrEqualTo(before);
    }
}
