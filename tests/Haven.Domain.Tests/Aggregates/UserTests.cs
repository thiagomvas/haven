using Haven.Domain.Aggregates;

using Shouldly;

namespace Haven.Domain.Tests.Aggregates;

[TestFixture]
[Category("Unit")]
public sealed class UserTests
{
    [Test]
    public void CreatePending_ShouldHaveEmptyNameAndPasswordHash_AndRequirePasswordChange()
    {
        var user = User.CreatePending("invitee@example.com");

        user.Name.ShouldBe(string.Empty);
        user.Email.ShouldBe("invitee@example.com");
        user.PasswordHash.ShouldBe(string.Empty);
        user.RequirePasswordChange.ShouldBeTrue();
        user.IsAdmin.ShouldBeFalse();
    }

    [Test]
    public void CreatePending_IsPendingInvite_ShouldBeTrue()
    {
        var user = User.CreatePending("invitee@example.com");

        user.IsPendingInvite.ShouldBeTrue();
    }

    [Test]
    public void CreatePending_WhenIsAdminTrue_ShouldCreateAdminUser()
    {
        var user = User.CreatePending("admin@example.com", isAdmin: true);

        user.IsAdmin.ShouldBeTrue();
    }

    [Test]
    public void Create_IsPendingInvite_ShouldBeFalse()
    {
        var user = User.Create("Alice", "alice@example.com", "hashed-password");

        user.IsPendingInvite.ShouldBeFalse();
        user.RequirePasswordChange.ShouldBeFalse();
    }

    [Test]
    public void AcceptInvite_ShouldSetNameAndPasswordHash_AndClearRequirePasswordChange()
    {
        var user = User.CreatePending("invitee@example.com");

        user.AcceptInvite("Invitee Name", "hashed-password");

        user.Name.ShouldBe("Invitee Name");
        user.PasswordHash.ShouldBe("hashed-password");
        user.RequirePasswordChange.ShouldBeFalse();
        user.IsPendingInvite.ShouldBeFalse();
    }
}