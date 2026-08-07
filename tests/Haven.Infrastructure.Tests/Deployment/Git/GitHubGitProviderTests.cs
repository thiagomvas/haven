using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Git;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GitHubGitProviderTests
{
    private ILogger<GitHubGitProvider> _logger = null!;
    private IGitHubOAuthService _oauthService = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IMemoryCache _cache = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GitHubGitProvider>>();
        _oauthService = Substitute.For<IGitHubOAuthService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _cache = Substitute.For<IMemoryCache>();
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
    }

    private GitHubGitProvider CreateSut(GitCredentials? credentials) =>
        new(credentials, _logger, _oauthService, _unitOfWork, _cache);

    [Test]
    public async Task GetAccessibleRepositoriesAsync_WithNullCredentials_Throws()
    {
        var sut = CreateSut(null);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetAccessibleRepositoriesAsync(CancellationToken.None));
    }

    [Test]
    public async Task GetBranchesAsync_WithNullCredentials_Throws()
    {
        var sut = CreateSut(null);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.GetBranchesAsync("https://github.com/octocat/Hello-World.git", CancellationToken.None));
    }

    [Test]
    public void ServiceType_ShouldBeGitHub()
    {
        var sut = CreateSut(GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds"));

        sut.Type.ShouldBe(GitProviderType.GitHub);
    }

    [Test]
    public async Task MarkValidatedIfStaleAsync_WithNullCredentials_DoesNotSave()
    {
        var sut = CreateSut(null);

        await sut.MarkValidatedIfStaleAsync(CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MarkValidatedIfStaleAsync_RecentlyValidated_DoesNotSave()
    {
        var credentials = GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds");
        var sut = CreateSut(credentials);

        await sut.MarkValidatedIfStaleAsync(CancellationToken.None);

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MarkValidatedIfStaleAsync_Stale_UpdatesTimestampAndSaves()
    {
        var credentials = GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds");
        SetLastValidatedAt(credentials, DateTimeOffset.UtcNow.AddHours(-2));
        var sut = CreateSut(credentials);

        await sut.MarkValidatedIfStaleAsync(CancellationToken.None);

        credentials.LastValidatedAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(-1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureCredentialsFreshAsync_WithTokenAuth_DoesNotRefresh()
    {
        var credentials = GitCredentials.CreateFromToken(GitProviderType.GitHub, null, "token", null, "Test Creds");
        var sut = new TestableGitHubGitProvider(credentials, _logger, _oauthService, _unitOfWork, _cache);

        await sut.CallEnsureCredentialsFreshAsync(CancellationToken.None);

        await _oauthService.DidNotReceive().RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureCredentialsFreshAsync_WithOAuthNotExpiring_DoesNotRefresh()
    {
        var credentials = GitCredentials.CreateFromOAuth(GitProviderType.GitHub, null, "access-token",
            "refresh-token", DateTimeOffset.UtcNow.AddHours(1), "Test Creds");
        var sut = new TestableGitHubGitProvider(credentials, _logger, _oauthService, _unitOfWork, _cache);

        await sut.CallEnsureCredentialsFreshAsync(CancellationToken.None);

        await _oauthService.DidNotReceive().RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureCredentialsFreshAsync_WithOAuthExpiringSoon_RefreshesAndSaves()
    {
        var credentials = GitCredentials.CreateFromOAuth(GitProviderType.GitHub, null, "access-token",
            "refresh-token", DateTimeOffset.UtcNow.AddSeconds(30), "Test Creds");
        _oauthService.RefreshTokenAsync("refresh-token", Arg.Any<CancellationToken>())
            .Returns(new GitHubOAuthTokenResult("new-access-token", "new-refresh-token", DateTimeOffset.UtcNow.AddHours(1)));

        var sut = new TestableGitHubGitProvider(credentials, _logger, _oauthService, _unitOfWork, _cache);

        await sut.CallEnsureCredentialsFreshAsync(CancellationToken.None);

        credentials.PrimaryCredential.Value.ShouldBe("new-access-token");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureCredentialsFreshAsync_WithOAuthAndNoRefreshToken_DoesNotRefresh()
    {
        var credentials = GitCredentials.CreateFromOAuth(GitProviderType.GitHub, null, "access-token",
            refreshToken: null, DateTimeOffset.UtcNow.AddSeconds(30), "Test Creds");
        var sut = new TestableGitHubGitProvider(credentials, _logger, _oauthService, _unitOfWork, _cache);

        await sut.CallEnsureCredentialsFreshAsync(CancellationToken.None);

        await _oauthService.DidNotReceive().RefreshTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EnsureCredentialsFreshAsync_WithNullCredentials_DoesNotThrow()
    {
        var sut = new TestableGitHubGitProvider(null, _logger, _oauthService, _unitOfWork, _cache);

        await Should.NotThrowAsync(() => sut.CallEnsureCredentialsFreshAsync(CancellationToken.None));
    }

    private static void SetLastValidatedAt(GitCredentials credentials, DateTimeOffset value)
    {
        var property = typeof(GitCredentials).GetProperty(nameof(GitCredentials.LastValidatedAt))!;
        property.SetValue(credentials, value);
    }

    private sealed class TestableGitHubGitProvider(
        GitCredentials? credentials,
        ILogger<GitHubGitProvider> logger,
        IGitHubOAuthService oauthService,
        IUnitOfWork unitOfWork,
        IMemoryCache cache) : GitHubGitProvider(credentials, logger, oauthService, unitOfWork, cache)
    {
        public Task CallEnsureCredentialsFreshAsync(CancellationToken cancellationToken) =>
            base.EnsureCredentialsFreshAsync(cancellationToken);
    }
}
