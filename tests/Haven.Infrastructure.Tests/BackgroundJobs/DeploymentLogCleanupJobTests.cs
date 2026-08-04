using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Infrastructure.BackgroundJobs;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using DeploymentEntity = Haven.Domain.Entities.Deployment;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class DeploymentLogCleanupJobTests
{
    private IDeploymentRepository _deploymentRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IOptionsMonitor<InstanceOptions> _instanceOptions = null!;
    private InstanceOptions _options = null!;
    private DeploymentLogCleanupJob _sut = null!;
    private string _basePath = null!;

    [SetUp]
    public void Setup()
    {
        _deploymentRepository = Substitute.For<IDeploymentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _instanceOptions = Substitute.For<IOptionsMonitor<InstanceOptions>>();

        _basePath = Path.Combine(Path.GetTempPath(), "haven-tests", Guid.NewGuid().ToString("N"));

        _options = new InstanceOptions
        {
            DeploymentLogRetentionCount = 10,
            DeploymentLogBasePath = _basePath
        };
        _instanceOptions.CurrentValue.Returns(_ => _options);

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity>());
        _deploymentRepository.FilterMissingIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        _sut = new DeploymentLogCleanupJob(
            _deploymentRepository,
            _unitOfWork,
            _instanceOptions,
            Substitute.For<ILogger<DeploymentLogCleanupJob>>());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }

    private string CreateLogFile(string fileName, string? directory = null)
    {
        var dir = directory ?? _basePath;
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, fileName);
        File.WriteAllText(path, "log contents");
        return path;
    }

    [Test]
    public async Task ExecuteAsync_WhenNoExcessDeployments_ShouldNotRemoveAnythingOrSaveChanges()
    {
        await _sut.ExecuteAsync();

        await _deploymentRepository.DidNotReceiveWithAnyArgs().RemoveAsync(default, default);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenNoExcessDeployments_ShouldSkipOrphanedFileScanEntirely()
    {
        // An orphaned log file sits on disk with no matching deployment row, but since there are
        // no excess deployments the job returns before ever scanning the log directory.
        var orphanedId = Guid.NewGuid();
        var orphanedFile = CreateLogFile($"{orphanedId}_run.log");

        await _sut.ExecuteAsync();

        await _deploymentRepository.DidNotReceiveWithAnyArgs().FilterMissingIdsAsync(default!, default);
        File.Exists(orphanedFile).ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_WhenExcessDeploymentHasExistingLogFile_ShouldDeleteFileAndRemoveDeployment()
    {
        var logFile = CreateLogFile("existing.log");
        var deployment = DeploymentEntity.Create(Guid.NewGuid(), logFile);

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { deployment });

        await _sut.ExecuteAsync();

        File.Exists(logFile).ShouldBeFalse();
        await _deploymentRepository.Received(1).RemoveAsync(deployment.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenExcessDeploymentLogFileDoesNotExistOnDisk_ShouldStillRemoveDeploymentWithoutThrowing()
    {
        var missingPath = Path.Combine(_basePath, "does-not-exist.log");
        var deployment = DeploymentEntity.Create(Guid.NewGuid(), missingPath);

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { deployment });

        await Should.NotThrowAsync(() => _sut.ExecuteAsync());

        await _deploymentRepository.Received(1).RemoveAsync(deployment.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenExcessDeploymentHasEmptyLogFilePath_ShouldSkipFileDeletionButStillRemoveDeployment()
    {
        var deployment = DeploymentEntity.Create(Guid.NewGuid(), string.Empty);

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { deployment });

        await Should.NotThrowAsync(() => _sut.ExecuteAsync());

        await _deploymentRepository.Received(1).RemoveAsync(deployment.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenMultipleExcessDeploymentsExist_ShouldRemoveEachOne()
    {
        var deployment1 = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("d1.log"));
        var deployment2 = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("d2.log"));

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { deployment1, deployment2 });

        await _sut.ExecuteAsync();

        await _deploymentRepository.Received(1).RemoveAsync(deployment1.Id, Arg.Any<CancellationToken>());
        await _deploymentRepository.Received(1).RemoveAsync(deployment2.Id, Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenBasePathDoesNotExistOnDisk_ShouldNotThrowAndStillFilterWithEmptyList()
    {
        // Gate the early return with one excess deployment so the orphan-scan phase actually runs.
        var deployment = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("d1.log"));
        Directory.Delete(_basePath, recursive: true);
        _options.DeploymentLogBasePath = Path.Combine(Path.GetTempPath(), "haven-tests-missing", Guid.NewGuid().ToString("N"));

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { deployment });

        await Should.NotThrowAsync(() => _sut.ExecuteAsync());

        await _deploymentRepository.Received(1).FilterMissingIdsAsync(
            Arg.Is<List<Guid>>(l => l.Count == 0), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenLogFileDeploymentIsMissingFromDatabase_ShouldDeleteOrphanedFile()
    {
        var gateDeployment = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("gate.log"));
        var orphanedId = Guid.NewGuid();
        var orphanedFile = CreateLogFile($"{orphanedId}_20240101.log");

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { gateDeployment });
        _deploymentRepository.FilterMissingIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { orphanedId });

        await _sut.ExecuteAsync();

        File.Exists(orphanedFile).ShouldBeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenLogFileDeploymentStillExistsInDatabase_ShouldNotDeleteFile()
    {
        var gateDeployment = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("gate.log"));
        var existingId = Guid.NewGuid();
        var existingFile = CreateLogFile($"{existingId}_20240101.log");

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { gateDeployment });
        _deploymentRepository.FilterMissingIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid>());

        await _sut.ExecuteAsync();

        File.Exists(existingFile).ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_WhenLogFileNameHasNoValidGuidPrefix_ShouldIgnoreItAndNotPassToFilter()
    {
        var gateDeployment = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("gate.log"));
        var malformedFile = CreateLogFile("not-a-guid.log");

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { gateDeployment });

        await _sut.ExecuteAsync();

        File.Exists(malformedFile).ShouldBeTrue();
        await _deploymentRepository.Received(1).FilterMissingIdsAsync(
            Arg.Is<List<Guid>>(l => l.Count == 0), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_WhenTwoLogFilesShareTheSameDeploymentIdPrefix_ShouldOnlyDeleteOneOfThem()
    {
        var gateDeployment = DeploymentEntity.Create(Guid.NewGuid(), CreateLogFile("gate.log"));
        var sharedId = Guid.NewGuid();

        // The dictionary is keyed by deployment id, so whichever of the two same-id files is
        // enumerated last silently overwrites the other's slot; only that one path is ever
        // deleted, and Directory.GetFiles ordering isn't guaranteed, so this locks in "exactly
        // one survives" rather than asserting which specific file that is.
        CreateLogFile($"{sharedId}_first.log");
        CreateLogFile($"{sharedId}_second.log");

        _deploymentRepository.GetExcessDeploymentsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<DeploymentEntity> { gateDeployment });
        _deploymentRepository.FilterMissingIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Guid> { sharedId });

        await _sut.ExecuteAsync();

        // gate.log is itself an excess deployment's log file, so phase 1 removes it regardless;
        // what this test locks in is that exactly one of the two same-id files survives phase 2.
        var remaining = Directory.GetFiles(_basePath).Select(Path.GetFileName).ToList();
        remaining.Count(f => f!.Contains(sharedId.ToString())).ShouldBe(1);
    }

    [Test]
    public async Task ExecuteAsync_ShouldPassConfiguredRetentionCountToRepository()
    {
        _options.DeploymentLogRetentionCount = 42;

        await _sut.ExecuteAsync();

        await _deploymentRepository.Received(1).GetExcessDeploymentsAsync(42, Arg.Any<CancellationToken>());
    }
}
