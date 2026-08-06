using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Infrastructure.Deployment.Git;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GitProviderFactoryTests
{
    private GitProviderFactory _sut = null!;
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _sut = new GitProviderFactory(
            _loggerFactory,
            Substitute.For<IGitHubOAuthService>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IMemoryCache>());
    }

    [TearDown]
    public void Dispose()
    {
        _loggerFactory.Dispose();
    }

    [Test]
    public void Create_WithGenericType_ReturnsProvider()
    {
        var provider = _sut.Create(GitProviderType.Generic);

        provider.ShouldNotBeNull();
        provider.Type.ShouldBe(GitProviderType.Generic);
    }

    [Test]
    public void Create_WithGitHubType_ReturnsGitHubProvider()
    {
        var provider = _sut.Create(GitProviderType.GitHub);

        provider.ShouldNotBeNull();
        provider.Type.ShouldBe(GitProviderType.GitHub);
        provider.ShouldBeOfType<GitHubGitProvider>();
    }

    [Test]
    public void Create_WithUnsupportedType_FallsBackToGenericProvider()
    {
        var unsupportedType = (GitProviderType)999;

        var provider = _sut.Create(unsupportedType);

        provider.ShouldNotBeNull();
        provider.Type.ShouldBe(GitProviderType.Generic);
    }
}