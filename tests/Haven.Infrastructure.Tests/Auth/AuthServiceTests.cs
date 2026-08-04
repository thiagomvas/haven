using System.IdentityModel.Tokens.Jwt;

using FastEndpoints;

using Haven.Application.Common;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Infrastructure.Auth;
using Haven.Infrastructure.Persistence;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Haven.Infrastructure.Tests.Auth;

[Category("Unit")]
public sealed class AuthServiceTests
{
    private HavenDbContext _db = null!;
    private IConfiguration _configuration = null!;
    private AuthService _sut = null!;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // JwtBearer.CreateToken resolves internal services through FastEndpoints' static
        // ServiceResolver, which is normally initialized by UseFastEndpoints() at host startup.
        // Outside a real host, this bootstraps a minimal service provider for it once per run.
        Factory.RegisterTestServices(_ => { });
    }

    [SetUp]
    public void Setup()
    {
        _db = TestDbContextFactory.CreateUnitDbContext();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "unit-test-secret-key-at-least-32-characters-long!",
                ["Jwt:Issuer"] = "Haven-Tests",
                ["Jwt:Audience"] = "Haven-Tests"
            })
            .Build();

        _sut = new AuthService(_db, _configuration);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    private static JwtSecurityToken DecodeAccessToken(string accessToken) =>
        new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

    [Test]
    public async Task RegisterAsync_ShouldReturnSuccessWithTokens()
    {
        var result = await _sut.RegisterAsync("Alice", "alice@example.com", "password123");

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrWhiteSpace();
        result.Value.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task RegisterAsync_ShouldPersistUserAsAdminWithHashedPassword()
    {
        const string plainPassword = "password123";

        await _sut.RegisterAsync("Alice", "alice@example.com", plainPassword);

        var user = await _db.Users.SingleAsync(u => u.Email == "alice@example.com");
        user.IsAdmin.ShouldBeTrue();
        user.RequirePasswordChange.ShouldBeFalse();
        user.PasswordHash.ShouldNotBe(plainPassword);
        BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash).ShouldBeTrue();
    }

    [Test]
    public async Task RegisterAsync_ShouldPersistExactlyOneActiveRefreshTokenForNewSession()
    {
        await _sut.RegisterAsync("Alice", "alice@example.com", "password123");

        var tokens = await _db.RefreshTokens.ToListAsync();
        tokens.Count.ShouldBe(1);
        tokens[0].IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task RegisterAsync_AccessTokenShouldContainExpectedClaims()
    {
        var result = await _sut.RegisterAsync("Alice", "alice@example.com", "password123");
        var user = await _db.Users.SingleAsync(u => u.Email == "alice@example.com");

        var jwt = DecodeAccessToken(result.Value.AccessToken);

        jwt.Claims.ShouldContain(c => c.Type == "sub" && c.Value == user.Id.ToString());
        jwt.Claims.ShouldContain(c => c.Type == "email" && c.Value == "alice@example.com");
        jwt.Claims.ShouldContain(c => c.Type == "name" && c.Value == "Alice");
        jwt.Claims.ShouldContain(c => c.Type == "role" && c.Value == "Admin");
        jwt.Issuer.ShouldBe("Haven-Tests");
    }

    [Test]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ShouldSucceedAndCreateASecondUserWithTheSameEmail()
    {
        // Neither AuthService nor UserConfiguration enforces email uniqueness (no unique index,
        // no pre-check), so this currently succeeds and leaves two distinct users sharing an
        // email. This test locks in that (surprising) behavior rather than a desired one.
        await _sut.RegisterAsync("Alice", "dup@example.com", "password123");

        var result = await _sut.RegisterAsync("Alice 2", "dup@example.com", "password456");

        result.IsSuccess.ShouldBeTrue();
        (await _db.Users.CountAsync(u => u.Email == "dup@example.com")).ShouldBe(2);
    }

    [Test]
    public async Task LoginAsync_WithCorrectCredentials_ShouldReturnTokens()
    {
        await _sut.RegisterAsync("Bob", "bob@example.com", "correct-password");

        var result = await _sut.LoginAsync("bob@example.com", "correct-password");

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ShouldReturnUnauthorized()
    {
        await _sut.RegisterAsync("Bob", "bob@example.com", "correct-password");

        var result = await _sut.LoginAsync("bob@example.com", "wrong-password");

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
    public async Task LoginAsync_IsCaseSensitiveOnEmail()
    {
        // Documents current behavior: lookup is an exact string match, so registering with
        // lowercase and logging in with a differently-cased email is treated as unknown.
        await _sut.RegisterAsync("Bob", "bob@example.com", "password123");

        var result = await _sut.LoginAsync("BOB@example.com", "password123");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task LoginAsync_ShouldIssueNewSessionDistinctFromRegistration()
    {
        await _sut.RegisterAsync("Bob", "bob@example.com", "password123");
        var registerSessionId = (await _db.RefreshTokens.SingleAsync()).SessionId;

        await _sut.LoginAsync("bob@example.com", "password123");

        _db.ChangeTracker.Clear();
        var sessions = await _db.RefreshTokens.AsNoTracking().Select(t => t.SessionId).Distinct().ToListAsync();
        sessions.Count.ShouldBe(2);
        sessions.ShouldContain(registerSessionId);
    }

    [Test]
    public async Task RefreshAsync_WithValidToken_ShouldReturnNewTokens()
    {
        var registerResult = await _sut.RegisterAsync("Carol", "carol@example.com", "password123");

        var result = await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefreshToken.ShouldNotBe(registerResult.Value.RefreshToken);
    }

    [Test]
    public async Task RefreshAsync_WithInvalidToken_ShouldReturnUnauthorized()
    {
        var result = await _sut.RefreshAsync("not-a-real-token");

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_ShouldRevokeConsumedToken()
    {
        var registerResult = await _sut.RegisterAsync("Dave", "dave@example.com", "password123");

        await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        _db.ChangeTracker.Clear();
        var original = await _db.RefreshTokens.AsNoTracking()
            .OrderBy(t => t.CreatedAt)
            .FirstAsync();
        original.IsRevoked.ShouldBeTrue();
    }

    [Test]
    public async Task RefreshAsync_ReusingAlreadyRotatedToken_ShouldReturnUnauthorized()
    {
        var registerResult = await _sut.RegisterAsync("Dave", "dave@example.com", "password123");
        var oldToken = registerResult.Value.RefreshToken;

        await _sut.RefreshAsync(oldToken);
        var reuseResult = await _sut.RefreshAsync(oldToken);

        reuseResult.IsFailure.ShouldBeTrue();
        reuseResult.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_WithExpiredToken_ShouldReturnUnauthorized()
    {
        var user = User.Create("Erin", "erin@example.com", BCrypt.Net.BCrypt.HashPassword("password123"));
        _db.Users.Add(user);

        const string rawToken = "expired-raw-token";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        var expiredToken = RefreshToken.Create(user.Id, Guid.NewGuid(), tokenHash, DateTime.UtcNow.AddDays(-1));
        _db.RefreshTokens.Add(expiredToken);
        await _db.SaveChangesAsync();

        var result = await _sut.RefreshAsync(rawToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_WhenUserWasDeletedAfterTokenIssued_ShouldReturnUnauthorized()
    {
        var user = User.Create("Frank", "frank@example.com", BCrypt.Net.BCrypt.HashPassword("password123"));
        _db.Users.Add(user);

        const string rawToken = "orphaned-raw-token";
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        var token = RefreshToken.Create(user.Id, Guid.NewGuid(), tokenHash, DateTime.UtcNow.AddDays(30));
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        var result = await _sut.RefreshAsync(rawToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task RefreshAsync_NewTokenShouldStayUnderSameSessionId()
    {
        var registerResult = await _sut.RegisterAsync("Grace", "grace@example.com", "password123");
        var originalSessionId = (await _db.RefreshTokens.SingleAsync()).SessionId;

        await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        _db.ChangeTracker.Clear();
        var activeToken = await _db.RefreshTokens.AsNoTracking().SingleAsync(t => !t.RevokedAt.HasValue);
        activeToken.SessionId.ShouldBe(originalSessionId);
    }

    [Test]
    public async Task LogoutAsync_ShouldRevokeAllActiveTokensForSession()
    {
        await _sut.RegisterAsync("Heidi", "heidi@example.com", "password123");
        var sessionId = (await _db.RefreshTokens.SingleAsync()).SessionId;

        var result = await _sut.LogoutAsync(sessionId);

        result.IsSuccess.ShouldBeTrue();
        _db.ChangeTracker.Clear();
        (await _db.RefreshTokens.AsNoTracking().AllAsync(t => t.RevokedAt.HasValue)).ShouldBeTrue();
    }

    [Test]
    public async Task LogoutAsync_ShouldNotAffectTokensFromOtherSessions()
    {
        await _sut.RegisterAsync("Heidi", "heidi@example.com", "password123");
        await _sut.RegisterAsync("Ivan", "ivan@example.com", "password123");
        var heidiUserId = (await _db.Users.SingleAsync(u => u.Email == "heidi@example.com")).Id;
        var heidiSessionId = (await _db.RefreshTokens.SingleAsync(t => t.UserId == heidiUserId)).SessionId;

        await _sut.LogoutAsync(heidiSessionId);

        _db.ChangeTracker.Clear();
        var ivanToken = await _db.RefreshTokens.AsNoTracking()
            .SingleAsync(t => t.SessionId != heidiSessionId);
        ivanToken.RevokedAt.ShouldBeNull();
    }

    [Test]
    public async Task LogoutAsync_WithUnknownSessionId_ShouldSucceedWithNoEffect()
    {
        var result = await _sut.LogoutAsync(Guid.NewGuid());

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task LogoutAsync_AfterLogout_RefreshTokenShouldBeUnusable()
    {
        var registerResult = await _sut.RegisterAsync("Judy", "judy@example.com", "password123");
        var sessionId = (await _db.RefreshTokens.SingleAsync()).SessionId;

        await _sut.LogoutAsync(sessionId);
        var result = await _sut.RefreshAsync(registerResult.Value.RefreshToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Unauthorized);
    }

    [Test]
    public async Task SetPasswordAsync_WithUnknownUserId_ShouldReturnNotFound()
    {
        var result = await _sut.SetPasswordAsync(Guid.NewGuid(), "new-password");

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(Error.NotFound.Code);
    }

    [Test]
    public async Task SetPasswordAsync_ShouldReplacePasswordHashAndClearRequirePasswordChange()
    {
        var createResult = await _sut.CreateUserAsync("Karl", "karl@example.com", "temp-password");
        var oldHash = (await _db.Users.SingleAsync(u => u.Id == createResult.Value)).PasswordHash;

        var result = await _sut.SetPasswordAsync(createResult.Value, "new-password");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
        _db.ChangeTracker.Clear();
        var user = await _db.Users.AsNoTracking().SingleAsync(u => u.Id == createResult.Value);
        user.PasswordHash.ShouldNotBe(oldHash);
        user.RequirePasswordChange.ShouldBeFalse();
        BCrypt.Net.BCrypt.Verify("new-password", user.PasswordHash).ShouldBeTrue();
    }

    [Test]
    public async Task SetPasswordAsync_OldPasswordShouldNoLongerWork()
    {
        await _sut.RegisterAsync("Laura", "laura@example.com", "old-password");
        var userId = (await _db.Users.SingleAsync(u => u.Email == "laura@example.com")).Id;

        await _sut.SetPasswordAsync(userId, "new-password");
        var loginWithOld = await _sut.LoginAsync("laura@example.com", "old-password");

        loginWithOld.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_ShouldPersistPendingUserRequiringPasswordChange()
    {
        var result = await _sut.CreateUserAsync("Mallory", "mallory@example.com", "temp-password");

        var user = await _db.Users.SingleAsync(u => u.Id == result.Value);
        user.RequirePasswordChange.ShouldBeTrue();
        user.IsAdmin.ShouldBeFalse();
        BCrypt.Net.BCrypt.Verify("temp-password", user.PasswordHash).ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_WhenIsAdminTrue_ShouldPersistAdminUser()
    {
        var result = await _sut.CreateUserAsync("Niaj", "niaj@example.com", "temp-password", isAdmin: true);

        var user = await _db.Users.SingleAsync(u => u.Id == result.Value);
        user.IsAdmin.ShouldBeTrue();
    }

    [Test]
    public async Task CreateUserAsync_CreatedUserShouldBeAbleToLoginWithTemporaryPassword()
    {
        await _sut.CreateUserAsync("Oscar", "oscar@example.com", "temp-password");

        var result = await _sut.LoginAsync("oscar@example.com", "temp-password");

        result.IsSuccess.ShouldBeTrue();
    }
}
