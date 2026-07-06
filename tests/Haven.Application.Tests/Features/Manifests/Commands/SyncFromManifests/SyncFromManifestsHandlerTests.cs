using Haven.Application.Common;
using Haven.Application.Common.Interfaces;
using Haven.Application.Features.Manifests.Commands.SyncFromManifests;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Haven.Application.Tests.Features.Manifests.Commands.SyncFromManifests;

[Category("Unit")]
[TestFixture]
public sealed class SyncFromManifestsHandlerTests
{
    private SyncFromManifestsHandler _sut = null!;
    private IManifestSyncService _syncService = null!;

    [SetUp]
    public void SetUp()
    {
        _syncService = Substitute.For<IManifestSyncService>();
        _sut = new SyncFromManifestsHandler(_syncService);
    }

    [Test]
    public async Task Handle_CallsSyncService()
    {
        var command = new SyncFromManifestsCommand();

        var result = await _sut.Handle(command, CancellationToken.None);

        await _syncService.Received(1).SyncAsync(Arg.Any<CancellationToken>());
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WhenSyncThrows_ReturnsFailure()
    {
        var command = new SyncFromManifestsCommand();
        var exception = new InvalidOperationException("Sync failed");
        _syncService.SyncAsync(Arg.Any<CancellationToken>()).Throws(exception);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_ThreadsCancellationTokenToService()
    {
        var command = new SyncFromManifestsCommand();
        var ct = new CancellationToken();

        await _sut.Handle(command, ct);

        await _syncService.Received(1).SyncAsync(ct);
    }
}