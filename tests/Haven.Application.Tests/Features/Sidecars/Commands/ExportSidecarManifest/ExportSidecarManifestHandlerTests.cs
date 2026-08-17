using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Sidecars.Commands.ExportSidecarManifest;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Sidecars.Commands.ExportSidecarManifest;

[Category("Unit")]
public sealed class ExportSidecarManifestHandlerTests
{
    private ISidecarRepository _sidecarRepository;
    private IManifestSerializer<Sidecar> _sidecarSerializer;
    private ExportSidecarManifestHandler _sut;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _sidecarSerializer = Substitute.For<IManifestSerializer<Sidecar>>();
        _sut = new ExportSidecarManifestHandler(_sidecarRepository, _sidecarSerializer);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenSidecarDoesNotExist()
    {
        var command = new ExportSidecarManifestCommand { SidecarId = Guid.NewGuid() };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>())
            .Returns((Sidecar?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        await _sidecarSerializer.DidNotReceive().WriteAsync(Arg.Any<Sidecar>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_ShouldWriteManifest_WhenSidecarExists()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        var command = new ExportSidecarManifestCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _sidecarSerializer.Received(1).WriteAsync(sidecar, Arg.Any<CancellationToken>());
    }
}
