using Haven.Application.Common;
using Haven.Application.Features.Backups.Commands.CreateBackup;
using Haven.Infrastructure.BackgroundJobs;

using Mediator;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.BackgroundJobs;

[Category("Unit")]
public sealed class BackupBackgroundJobTests
{
    private IMediator _mediator = null!;
    private ILogger<BackupBackgroundJob> _logger = null!;
    private BackupBackgroundJob _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mediator = Substitute.For<IMediator>();
        _logger = Substitute.For<ILogger<BackupBackgroundJob>>();
        _sut = new BackupBackgroundJob(_mediator, _logger);
    }

    [Test(Description = "ExecuteAsync should send a CreateBackupCommand through the mediator")]
    public async Task ExecuteAsync_SendsCreateBackupCommand()
    {
        _mediator.Send(Arg.Any<CreateBackupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateBackupResult>.CreatedFor(new CreateBackupResult("/backups/20240101-120000", DateTimeOffset.UtcNow)));

        await _sut.ExecuteAsync();

        await _mediator.Received(1).Send(Arg.Any<CreateBackupCommand>(), Arg.Any<CancellationToken>());
    }

    [Test(Description = "When the backup command succeeds an information log with the snapshot path is emitted")]
    public async Task ExecuteAsync_WhenBackupSucceeds_LogsSuccessWithSnapshotPath()
    {
        const string snapshotPath = "/backups/20240101-120000";
        _mediator.Send(Arg.Any<CreateBackupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateBackupResult>.CreatedFor(new CreateBackupResult(snapshotPath, DateTimeOffset.UtcNow)));

        await _sut.ExecuteAsync();

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(x => x.ToString()!.Contains(snapshotPath)),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test(Description = "When the backup command fails an error log containing the error description is emitted")]
    public async Task ExecuteAsync_WhenBackupFails_LogsError()
    {
        var error = Error.Failure("Backup.Failed", "Disk is full");
        _mediator.Send(Arg.Any<CreateBackupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateBackupResult>.Failure(error));

        await _sut.ExecuteAsync();

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Test(Description = "When the backup command succeeds no error log should be emitted")]
    public async Task ExecuteAsync_WhenBackupSucceeds_DoesNotLogError()
    {
        _mediator.Send(Arg.Any<CreateBackupCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<CreateBackupResult>.CreatedFor(new CreateBackupResult("/backups/snap", DateTimeOffset.UtcNow)));

        await _sut.ExecuteAsync();

        _logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
