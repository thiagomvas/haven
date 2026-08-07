using Haven.Domain.Entities;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class UserInviteTokenTests
{
    [Test]
    public void Create_ShouldSetFieldsAndBeActive()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(72);

        var token = UserInviteToken.Create(userId, "hashed-token", expiresAt);

        token.UserId.ShouldBe(userId);
        token.TokenHash.ShouldBe("hashed-token");
        token.ExpiresAt.ShouldBe(expiresAt);
        token.IsExpired.ShouldBeFalse();
        token.IsRevoked.ShouldBeFalse();
        token.IsAccepted.ShouldBeFalse();
        token.IsActive.ShouldBeTrue();
    }

    [Test]
    public void IsExpired_WhenExpiresAtInPast_ShouldBeTrue_AndTokenShouldNotBeActive()
    {
        var token = UserInviteToken.Create(Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddMinutes(-1));

        token.IsExpired.ShouldBeTrue();
        token.IsActive.ShouldBeFalse();
    }

    [Test]
    public void Revoke_ShouldSetRevokedAt_AndTokenShouldNotBeActive()
    {
        var token = UserInviteToken.Create(Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddHours(72));

        token.Revoke();

        token.IsRevoked.ShouldBeTrue();
        token.IsActive.ShouldBeFalse();
    }

    [Test]
    public void MarkAccepted_ShouldSetAcceptedAt_AndTokenShouldNotBeActive()
    {
        var token = UserInviteToken.Create(Guid.NewGuid(), "hashed-token", DateTime.UtcNow.AddHours(72));

        token.MarkAccepted();

        token.IsAccepted.ShouldBeTrue();
        token.IsActive.ShouldBeFalse();
    }
}