using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Infrastructure.Deployment.Git;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GitProviderFactoryTests
{
    private GitProviderFactory _sut = null!;
    private IEncryptionService _encryptionService;
    private ILoggerFactory _loggerFactory;

    [SetUp]
    public void Setup()
    {
        _encryptionService = Substitute.For<IEncryptionService>();
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());

        _sut = new GitProviderFactory(_encryptionService, _loggerFactory);
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
    public void Create_WithUnsupportedType_ThrowsNotSupportedException()
    {
        var unsupportedType = (GitProviderType)999;

        Should.Throw<NotSupportedException>(() => _sut.Create(unsupportedType));
    }
}