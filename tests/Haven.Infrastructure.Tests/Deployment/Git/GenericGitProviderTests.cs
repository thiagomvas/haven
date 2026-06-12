using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Git;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GenericGitProviderTests
{
    private GenericGitProvider _sut = null!;
    private ILogger<GenericGitProvider> _logger;
    private GitCredentials _credentials;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GenericGitProvider>>();
        _credentials = GitCredentials.CreateFromToken(GitProviderType.Generic, null, "test-token", null, "Test Creds");

        _sut = new GenericGitProvider(_credentials, _logger);
    }

    [Test]
    public void ServiceType_ShouldBeGeneric()
    {
        _sut.Type.ShouldBe(GitProviderType.Generic);
    }

    [Test]
    public async Task PullAsync_WithInvalidPath_ShouldThrowInvalidOperationException()
    {
        var invalidPath = "/non/existent/path";

        await Should.ThrowAsync<InvalidOperationException>(
            () => _sut.PullAsync(invalidPath, "main", CancellationToken.None));
    }

    [Test]
    public async Task GetBranchesAsync_WithInvalidUrl_ShouldThrowLibGit2SharpException()
    {
        var invalidUrl = "https://invalid-url-that-does-not-exist.example.com/repo.git";

        await Should.ThrowAsync<LibGit2SharpException>(
            () => _sut.GetBranchesAsync(invalidUrl, CancellationToken.None));
    }

    [Test]
    public async Task GetBranchesAsync_WithoutCredentials_ShouldCallGetBranches()
    {
        var sutNoCredentials = new GenericGitProvider(null, _logger);
        var invalidUrl = "https://invalid-url-that-does-not-exist.example.com/repo.git";

        await Should.ThrowAsync<LibGit2SharpException>(
            () => sutNoCredentials.GetBranchesAsync(invalidUrl, CancellationToken.None));
    }
}