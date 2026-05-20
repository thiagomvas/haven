using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Git;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GitServiceTests
{
    private GitService _sut = null!;
    private IGitRepositoryPathProvider _repositoryPathProvider;
    private IGitCredentialsRepository _credentialsRepository;
    private IGitProviderFactory _gitProviderFactory;
    private IGitProvider _gitProvider;
    private ILogger<GitService> _logger;

    private string _tempRoot = null!;

    [SetUp]
    public void Setup()
    {
        _repositoryPathProvider = Substitute.For<IGitRepositoryPathProvider>();
        _credentialsRepository = Substitute.For<IGitCredentialsRepository>();
        _gitProviderFactory = Substitute.For<IGitProviderFactory>();
        _gitProvider = Substitute.For<IGitProvider>();
        _logger = Substitute.For<ILogger<GitService>>();

        _tempRoot = Path.Combine(Path.GetTempPath(), $"haven-git-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRoot);

        _gitProviderFactory.Create(Arg.Any<GitProviderType>(), Arg.Any<GitCredentials?>())
            .Returns(_gitProvider);

        _repositoryPathProvider.GetServiceRepositoryPath(Arg.Any<Guid>())
            .Returns(x => Path.Combine(_tempRoot, "services", x[0].ToString()!));

        _sut = new GitService(_repositoryPathProvider, _credentialsRepository, _gitProviderFactory, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    [Test]
    public async Task CloneServiceRepository_WhenSuccessful_ReturnsPath()
    {
        var serviceId = Guid.NewGuid();
        var repositoryUrl = "https://github.com/example/repo.git";
        var expectedPath = Path.Combine(_tempRoot, "services", serviceId.ToString());

        _repositoryPathProvider.GetServiceRepositoryPath(serviceId).Returns(expectedPath);
        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(false);
        _gitProvider.CloneRepositoryAsync(repositoryUrl, expectedPath, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.CloneServiceRepositoryAsync(serviceId, repositoryUrl);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedPath);
    }

    [Test]
    public async Task CloneServiceRepository_WhenProviderThrows_ReturnsFailure()
    {
        var serviceId = Guid.NewGuid();
        var repositoryUrl = "https://github.com/example/repo.git";

        _gitProvider.CloneRepositoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Clone failed"));

        var result = await _sut.CloneServiceRepositoryAsync(serviceId, repositoryUrl);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task CloneServiceRepository_WhenProviderThrows_LogsError()
    {
        var serviceId = Guid.NewGuid();

        _gitProvider.CloneRepositoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Clone failed"));

        await _sut.CloneServiceRepositoryAsync(serviceId, "https://github.com/example/repo.git");

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test]
    public async Task PullServiceRepository_WhenRepositoryExists_ReturnsSuccess()
    {
        var serviceId = Guid.NewGuid();
        var branch = "main";

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(true);
        _gitProvider.PullAsync(Arg.Any<string>(), branch, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.PullServiceRepositoryAsync(serviceId, branch);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task PullServiceRepository_WhenRepositoryDoesNotExist_ReturnsNotFound()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(false);

        var result = await _sut.PullServiceRepositoryAsync(serviceId, "main");

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task PullServiceRepository_WhenProviderThrows_ReturnsFailure()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(true);
        _gitProvider.PullAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Pull failed"));

        var result = await _sut.PullServiceRepositoryAsync(serviceId, "main");

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task GetRemoteBranches_WhenSuccessful_ReturnsBranches()
    {
        var repositoryUrl = "https://github.com/example/repo.git";
        var branches = new List<string> { "main", "develop", "feature-1" };

        _gitProvider.GetBranchesAsync(repositoryUrl, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(branches));

        var result = await _sut.GetRemoteBranchesAsync(repositoryUrl);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(branches);
    }

    [Test]
    public async Task GetRemoteBranches_WhenProviderThrows_ReturnsFailure()
    {
        var repositoryUrl = "https://github.com/example/repo.git";

        _gitProvider.GetBranchesAsync(repositoryUrl, Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Get branches failed"));

        var result = await _sut.GetRemoteBranchesAsync(repositoryUrl);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public void GetServiceRepositoryPath_WhenRepositoryExists_ReturnsPath()
    {
        var serviceId = Guid.NewGuid();
        var expectedPath = Path.Combine(_tempRoot, "services", serviceId.ToString());

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(true);
        _repositoryPathProvider.GetServiceRepositoryPath(serviceId).Returns(expectedPath);

        var result = _sut.GetServiceRepositoryPath(serviceId);

        result.ShouldBe(expectedPath);
    }

    [Test]
    public void GetServiceRepositoryPath_WhenRepositoryDoesNotExist_ReturnsNull()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(false);

        var result = _sut.GetServiceRepositoryPath(serviceId);

        result.ShouldBeNull();
    }

    [Test]
    public void ServiceRepositoryExists_WhenRepositoryExists_ReturnsTrue()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(true);

        var result = _sut.ServiceRepositoryExists(serviceId);

        result.ShouldBeTrue();
    }

    [Test]
    public void ServiceRepositoryExists_WhenRepositoryDoesNotExist_ReturnsFalse()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.RepositoryDirectoryExists(serviceId).Returns(false);

        var result = _sut.ServiceRepositoryExists(serviceId);

        result.ShouldBeFalse();
    }

    [Test]
    public async Task DeleteServiceRepository_WhenRepositoryExists_CompletesWithoutThrowing()
    {
        var serviceId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), $"haven-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "content");

        _repositoryPathProvider.GetServiceRepositoryPath(serviceId).Returns(tempDir);

        try
        {
            await Should.NotThrowAsync(() => _sut.DeleteServiceRepositoryAsync(serviceId));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task DeleteServiceRepository_WhenRepositoryDoesNotExist_DoesNotThrow()
    {
        var serviceId = Guid.NewGuid();

        _repositoryPathProvider.GetServiceRepositoryPath(serviceId).Returns("/nonexistent/path");

        await Should.NotThrowAsync(() => _sut.DeleteServiceRepositoryAsync(serviceId));
    }
}
