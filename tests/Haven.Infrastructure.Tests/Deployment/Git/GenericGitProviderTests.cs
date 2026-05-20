using Haven.Application.Common.Interfaces;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Git;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GenericGitProviderTests
{
    private GenericGitProvider _sut = null!;
    private IEncryptionService _encryptionService;
    private ILogger<GenericGitProvider> _logger;
    private GitCredentials _credentials;

    [SetUp]
    public void Setup()
    {
        _encryptionService = Substitute.For<IEncryptionService>();
        _logger = Substitute.For<ILogger<GenericGitProvider>>();
        _credentials = GitCredentials.CreateFromToken(GitProviderType.Generic, null, "test-token", null, "Test Creds");

        _sut = new GenericGitProvider(_credentials, _encryptionService, _logger);
    }

    [Test]
    public void ServiceType_ShouldBeGeneric()
    {
        _sut.Type.ShouldBe(GitProviderType.Generic);
    }

    [Test]
    public async Task PullAsync_ShouldThrowNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => _sut.PullAsync("/some/path", "main", CancellationToken.None));
    }

    [Test]
    public async Task GetBranchesAsync_ShouldThrowNotImplementedException()
    {
        await Should.ThrowAsync<NotImplementedException>(
            () => _sut.GetBranchesAsync("https://github.com/example/repo.git", CancellationToken.None));
    }
}
