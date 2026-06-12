using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Configuration;
using Haven.Application.Features.Backups.Commands.CreateBackup;
using Haven.Domain;
using Haven.Domain.Entities;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Backups.Commands.CreateBackup;

[Category("Unit")]
public sealed class CreateBackupHandlerTests
{
    private IBackupManifestWriter _backupManifestWriter = null!;
    private IGitProviderFactory _gitProviderFactory = null!;
    private IGitCredentialsRepository _gitCredentialsRepository = null!;
    private IOptionsMonitor<BackupOptions> _backupOptions = null!;
    private IOptionsMonitor<ManifestsOptions> _manifestsOptions = null!;
    private CreateBackupHandler _sut = null!;

    private string _backupsPath = null!;
    private string _manifestsPath = null!;

    [SetUp]
    public void Setup()
    {
        _backupsPath = Path.Combine(Path.GetTempPath(), $"haven-backups-{Guid.NewGuid()}");
        _manifestsPath = Path.Combine(Path.GetTempPath(), $"haven-manifests-{Guid.NewGuid()}");

        _backupManifestWriter = Substitute.For<IBackupManifestWriter>();
        _gitProviderFactory = Substitute.For<IGitProviderFactory>();
        _gitCredentialsRepository = Substitute.For<IGitCredentialsRepository>();

        _backupOptions = Substitute.For<IOptionsMonitor<BackupOptions>>();
        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = false }
        });

        _manifestsOptions = Substitute.For<IOptionsMonitor<ManifestsOptions>>();
        _manifestsOptions.CurrentValue.Returns(new ManifestsOptions
        {
            ManifestsPath = _manifestsPath
        });

        _sut = new CreateBackupHandler(
            _backupManifestWriter,
            _gitProviderFactory,
            _gitCredentialsRepository,
            _backupOptions,
            _manifestsOptions);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_backupsPath))
            Directory.Delete(_backupsPath, recursive: true);
        if (Directory.Exists(_manifestsPath))
            Directory.Delete(_manifestsPath, recursive: true);
    }

    [Test(Description = "WriteAllAsync should be called once with a path rooted under BackupsPath for the timestamped snapshot")]
    public async Task Handle_WritesSnapshotToSnapshotDirectory()
    {
        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await _backupManifestWriter.Received(1).WriteAllAsync(
            Arg.Is<string>(p => p.StartsWith(_backupsPath)),
            Arg.Any<CancellationToken>());
    }

    [Test(Description = "WriteAllAsync should be called once with the configured ManifestsPath to keep manifests in sync")]
    public async Task Handle_WritesManifestsToManifestsPath()
    {
        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await _backupManifestWriter.Received(1).WriteAllAsync(
            _manifestsPath,
            Arg.Any<CancellationToken>());
    }

    [Test(Description = "WriteAllAsync is called exactly twice per backup: once for the snapshot and once for the live manifests directory")]
    public async Task Handle_CallsWriteAllAsyncTwice()
    {
        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await _backupManifestWriter.Received(2).WriteAllAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Test(Description = "A successful backup returns a CreatedFor result containing the snapshot path and a timestamp")]
    public async Task Handle_ReturnsSuccessResultWithSnapshotPathAndTimestamp()
    {
        var before = DateTimeOffset.UtcNow;

        var result = await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        var after = DateTimeOffset.UtcNow;

        result.IsSuccess.ShouldBeTrue();
        result.Value.SnapshotPath.StartsWith(_backupsPath).ShouldBeTrue();
        result.Value.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        result.Value.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Test(Description = "When existing snapshots exceed RetentionCount the oldest ones are deleted, keeping only the newest N")]
    public async Task Handle_WithRetentionCountExceeded_DeletesOldestSnapshots()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 2,
            Git = new BackupGitOptions { Enabled = false }
        });

        Directory.CreateDirectory(_backupsPath);
        // Names are date-like strings; descending sort keeps the largest (newest) first.
        var old1 = Directory.CreateDirectory(Path.Combine(_backupsPath, "20240101-000000")).FullName;
        var old2 = Directory.CreateDirectory(Path.Combine(_backupsPath, "20240102-000000")).FullName;
        var newer1 = Directory.CreateDirectory(Path.Combine(_backupsPath, "20240103-000000")).FullName;
        var newer2 = Directory.CreateDirectory(Path.Combine(_backupsPath, "20240104-000000")).FullName;

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        // The handler creates one more snapshot dir during the run, so after retention there should be exactly RetentionCount dirs.
        var remaining = Directory.GetDirectories(_backupsPath);
        remaining.Length.ShouldBe(2);
        remaining.ShouldNotContain(old1);
        remaining.ShouldNotContain(old2);
    }

    [Test(Description = "When RetentionCount is not exceeded no snapshot directories are deleted")]
    public async Task Handle_WithinRetentionCount_DoesNotDeleteAnySnapshot()
    {
        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = false }
        });

        Directory.CreateDirectory(_backupsPath);
        var existing = Directory.CreateDirectory(Path.Combine(_backupsPath, "20240101-000000")).FullName;

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        Directory.Exists(existing).ShouldBeTrue();
    }

    [Test(Description = "When Git integration is disabled the git provider factory should never be called")]
    public async Task Handle_WithGitDisabled_DoesNotInteractWithGitProvider()
    {
        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        _gitProviderFactory.DidNotReceive().Create(Arg.Any<GitProviderType>(), Arg.Any<GitCredentials?>());
    }

    [Test(Description = "When Git is enabled InitRepositoryAsync and CommitAsync are called on the created provider")]
    public async Task Handle_WithGitEnabled_InitializesAndCommits()
    {
        var gitProvider = Substitute.For<IGitProvider>();
        _gitProviderFactory.Create(Arg.Any<GitProviderType>(), Arg.Any<GitCredentials?>()).Returns(gitProvider);

        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = true, Branch = "main" }
        });

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await gitProvider.Received(1).InitRepositoryAsync(_manifestsPath, Arg.Any<CancellationToken>());
        await gitProvider.Received(1).CommitAsync(
            _manifestsPath,
            Arg.Any<string>(),
            "main",
            Arg.Any<CancellationToken>());
    }

    [Test(Description = "When Git is enabled but RemoteUrl is null PushAsync should not be called")]
    public async Task Handle_WithGitEnabledAndNoRemoteUrl_DoesNotPush()
    {
        var gitProvider = Substitute.For<IGitProvider>();
        _gitProviderFactory.Create(Arg.Any<GitProviderType>(), Arg.Any<GitCredentials?>()).Returns(gitProvider);

        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = true, RemoteUrl = null }
        });

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await gitProvider.DidNotReceive().PushAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test(Description = "When Git is enabled and RemoteUrl is set PushAsync is called with the manifests path and remote URL")]
    public async Task Handle_WithGitEnabledAndRemoteUrl_Pushes()
    {
        const string remoteUrl = "https://github.com/org/repo.git";
        var gitProvider = Substitute.For<IGitProvider>();
        _gitProviderFactory.Create(Arg.Any<GitProviderType>(), Arg.Any<GitCredentials?>()).Returns(gitProvider);

        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = true, RemoteUrl = remoteUrl, Branch = "main" }
        });

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await gitProvider.Received(1).PushAsync(
            _manifestsPath,
            remoteUrl,
            "main",
            Arg.Any<CancellationToken>());
    }

    [Test(Description = "When GitCredentialsId is set the credentials are fetched and passed to the provider factory")]
    public async Task Handle_WithGitCredentialsId_FetchesAndPassesCredentialsToFactory()
    {
        var credentialsId = Guid.NewGuid();
        var credentials = GitCredentials.CreateFromToken(GitProviderType.Generic, null, "token", null, "Test");
        _gitCredentialsRepository.GetByIdAsync(credentialsId, Arg.Any<CancellationToken>())
            .Returns(credentials);

        var gitProvider = Substitute.For<IGitProvider>();
        _gitProviderFactory.Create(Arg.Any<GitProviderType>(), credentials).Returns(gitProvider);

        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions
            {
                Enabled = true,
                GitCredentialsId = credentialsId
            }
        });

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await _gitCredentialsRepository.Received(1).GetByIdAsync(credentialsId, Arg.Any<CancellationToken>());
        _gitProviderFactory.Received(1).Create(GitProviderType.Generic, credentials);
    }

    [Test(Description = "When no GitCredentialsId is configured the factory is called with null credentials and Generic provider type")]
    public async Task Handle_WithNoGitCredentialsId_CreatesProviderWithNullCredentials()
    {
        var gitProvider = Substitute.For<IGitProvider>();
        _gitProviderFactory.Create(GitProviderType.Generic, null).Returns(gitProvider);

        _backupOptions.CurrentValue.Returns(new BackupOptions
        {
            BackupsPath = _backupsPath,
            RetentionCount = 10,
            Git = new BackupGitOptions { Enabled = true, GitCredentialsId = null }
        });

        await _sut.Handle(new CreateBackupCommand(), CancellationToken.None);

        await _gitCredentialsRepository.DidNotReceive()
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        _gitProviderFactory.Received(1).Create(GitProviderType.Generic, null);
    }
}
