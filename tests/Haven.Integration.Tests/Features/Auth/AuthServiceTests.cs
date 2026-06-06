using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Auth;
using Haven.Infrastructure.Persistence;
using Haven.Integration.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Haven.Integration.Tests.Features.Auth;

[TestFixture]
[Category("Integration")]
public class AuthServiceTests
{
    private IntegrationTestFixture _fixture = null!;
    private IAuthService _sut = null!;
    private HavenDbContext _db = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
        _sut = _fixture.GetService<IAuthService>();
        _db = _fixture.GetService<HavenDbContext>();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _fixture?.Dispose();
    }

    [Test]
    public async Task RegisterAsync_WithValidCredentials_ShouldReturnTokens()
    {
        var result = await _sut.RegisterAsync("Alice", "alice@example.com", "password123");

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task RegisterAsync_ShouldPersistUserToDatabase()
    {
        await _sut.RegisterAsync("Bob", "bob@example.com", "password123");

        _db.Users.Any(u => u.Email == "bob@example.com").ShouldBeTrue();
    }

    [Test]
    public async Task RegisterAsync_ShouldPersistRefreshTokenToDatabase()
    {
        await _sut.RegisterAsync("Carol", "carol@example.com", "password123");

        _db.RefreshTokens.Any().ShouldBeTrue();
    }

    [Test]
    public async Task RegisterAsync_ShouldHashPasswordBeforeStoring()
    {
        const string plainPassword = "my-secret";

        await _sut.RegisterAsync("Dave", "dave@example.com", plainPassword);

        var user = _db.Users.Single(u => u.Email == "dave@example.com");
        user.PasswordHash.ShouldNotBe(plainPassword);
    }

    [Test]
    public async Task RegisterAsync_ShouldCreateUserWithRequirePasswordChangeFalse()
    {
        await _sut.RegisterAsync("Eve", "eve@example.com", "password123");

        var user = _db.Users.Single(u => u.Email == "eve@example.com");
        user.RequirePasswordChange.ShouldBeFalse();
    }

    [Test]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnTokens()
    {
        await _sut.RegisterAsync("Frank", "frank@example.com", "correct-password");

        var result = await _sut.LoginAsync("frank@example.com", "correct-password");

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ShouldReturnUnauthorized()
    {
        await _sut.RegisterAsync("Grace", "grace@example.com", "correct-password");

        var result = await _sut.LoginAsync("grace@example.com", "wrong-password");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_WithUnknownEmail_ShouldReturnUnauthorized()
    {
        var result = await _sut.LoginAsync("nobody@example.com", "password");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_ShouldPersistNewRefreshToken()
    {
        await _sut.RegisterAsync("Heidi", "heidi@example.com", "password");
        var tokensBefore = _db.RefreshTokens.Count();

        await _sut.LoginAsync("heidi@example.com", "password");

        _db.RefreshTokens.Count().ShouldBeGreaterThan(tokensBefore);
    }

    [Test]
    public async Task RefreshAsync_WithValidToken_ShouldReturnNewTokens()
    {
        var registerResult = await _sut.RegisterAsync("Ivan", "ivan@example.com", "password");

        var result = await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task RefreshAsync_ShouldRotateToken_OldTokenBecomesRevoked()
    {
        var registerResult = await _sut.RegisterAsync("Judy", "judy@example.com", "password");
        var oldToken = registerResult.Value.RefreshToken;

        await _sut.RefreshAsync(oldToken);
        var reuseResult = await _sut.RefreshAsync(oldToken);

        reuseResult.IsFailure.ShouldBeTrue();
        reuseResult.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var result = await _sut.RefreshAsync("not-a-real-token");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_NewTokenIssuedUnderSameSession()
    {
        var registerResult = await _sut.RegisterAsync("Karl", "karl@example.com", "password");
        var originalSessionId = _db.RefreshTokens.Single().SessionId;

        await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        _db.ChangeTracker.Clear();
        _db.RefreshTokens.AsNoTracking().Where(t => !t.RevokedAt.HasValue).Single().SessionId
            .ShouldBe(originalSessionId);
    }

    [Test]
    public async Task LogoutAsync_ShouldRevokeAllActiveTokensForSession()
    {
        await _sut.RegisterAsync("Lara", "lara@example.com", "password");
        var sessionId = _db.RefreshTokens.Single().SessionId;

        await _sut.LogoutAsync(sessionId);

        _db.ChangeTracker.Clear();
        _db.RefreshTokens.AsNoTracking().All(t => t.RevokedAt.HasValue).ShouldBeTrue();
    }

    [Test]
    public async Task LogoutAsync_WithUnknownSessionId_ShouldSucceedWithNoEffect()
    {
        var result = await _sut.LogoutAsync(Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task LogoutAsync_AfterLogout_RefreshTokenShouldBeInvalid()
    {
        var registerResult = await _sut.RegisterAsync("Mike", "mike@example.com", "password");
        var sessionId = _db.RefreshTokens.Single().SessionId;

        await _sut.LogoutAsync(sessionId);
        var result = await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task SetPasswordAsync_WithValidUserId_ShouldReturnSuccess()
    {
        await _sut.RegisterAsync("Nina", "nina@example.com", "old-password");
        var userId = _db.Users.Single(u => u.Email == "nina@example.com").Id;

        var result = await _sut.SetPasswordAsync(userId, "new-password");

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task SetPasswordAsync_ShouldAllowLoginWithNewPassword()
    {
        await _sut.RegisterAsync("Otto", "otto@example.com", "old-password");
        var userId = _db.Users.Single(u => u.Email == "otto@example.com").Id;

        await _sut.SetPasswordAsync(userId, "new-password");
        var loginResult = await _sut.LoginAsync("otto@example.com", "new-password");

        loginResult.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task SetPasswordAsync_ShouldRejectOldPasswordAfterChange()
    {
        await _sut.RegisterAsync("Petra", "petra@example.com", "old-password");
        var userId = _db.Users.Single(u => u.Email == "petra@example.com").Id;

        await _sut.SetPasswordAsync(userId, "new-password");
        var loginResult = await _sut.LoginAsync("petra@example.com", "old-password");

        loginResult.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task SetPasswordAsync_ShouldClearRequirePasswordChangeFlag()
    {
        await _sut.CreateUserAsync("Quinn", "quinn@example.com", "temp-password");
        var userId = _db.Users.Single(u => u.Email == "quinn@example.com").Id;

        await _sut.SetPasswordAsync(userId, "new-password");

        _db.ChangeTracker.Clear();
        _db.Users.AsNoTracking().Single(u => u.Id == userId).RequirePasswordChange.ShouldBeFalse();
    }

    [Test]
    public async Task SetPasswordAsync_WithUnknownUserId_ShouldReturnNotFound()
    {
        var result = await _sut.SetPasswordAsync(Guid.NewGuid(), "new-password");

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_ShouldReturnNewUserId()
    {
        var result = await _sut.CreateUserAsync("Ray", "ray@example.com", "temp");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public async Task CreateUserAsync_ShouldPersistUserWithRequirePasswordChangeTrue()
    {
        await _sut.CreateUserAsync("Sam", "sam@example.com", "temp");

        _db.Users.Single(u => u.Email == "sam@example.com").RequirePasswordChange.ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_WhenIsAdminTrue_ShouldCreateAdminUser()
    {
        await _sut.CreateUserAsync("Tara", "tara@example.com", "temp", isAdmin: true);

        _db.Users.Single(u => u.Email == "tara@example.com").IsAdmin.ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_DefaultIsNotAdmin()
    {
        await _sut.CreateUserAsync("Uma", "uma@example.com", "temp");

        _db.Users.Single(u => u.Email == "uma@example.com").IsAdmin.ShouldBeFalse();
    }
}
