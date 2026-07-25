using Haven.Application.Common;
using Haven.Application.Features.Backups.Commands.RestoreBackup;
using Haven.Application.Features.Manifests.Commands.SyncFromManifests;

using Mediator;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Manifests.Commands.SyncFromManifests;

[Category("Unit")]
[TestFixture]
public sealed class SyncFromManifestsHandlerTests
{
    private SyncFromManifestsHandler _sut = null!;
    private IMediator _sender = null!;

    [SetUp]
    public void SetUp()
    {
        _sender = Substitute.For<IMediator>();
        _sut = new SyncFromManifestsHandler(_sender);
    }

    [Test]
    public async Task Handle_DelegatesToRestoreBackupCommandWithManifestSource()
    {
        var expected = Result<RestoreBackupResult>.Success(new RestoreBackupResult { DryRun = false, Projects = new EntityChangeSummary<ProjectRestoreItem>() });
        _sender.Send(Arg.Any<RestoreBackupCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        var command = new SyncFromManifestsCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RestoreBackupCommand>(c => c.Source == RestoreSource.Manifest && c.DryRun == false),
            Arg.Any<CancellationToken>());
        result.ShouldBe(expected);
    }

    [Test]
    public async Task Handle_ThreadsDryRunToRestoreBackupCommand()
    {
        var expected = Result<RestoreBackupResult>.Success(new RestoreBackupResult { DryRun = true, Projects = new EntityChangeSummary<ProjectRestoreItem>() });
        _sender.Send(Arg.Any<RestoreBackupCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        var command = new SyncFromManifestsCommand(DryRun: true);

        await _sut.Handle(command, CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RestoreBackupCommand>(c => c.DryRun == true),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WhenRestoreFails_ReturnsFailure()
    {
        var expected = Result<RestoreBackupResult>.Failure(Error.Failed);
        _sender.Send(Arg.Any<RestoreBackupCommand>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.Handle(new SyncFromManifestsCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
