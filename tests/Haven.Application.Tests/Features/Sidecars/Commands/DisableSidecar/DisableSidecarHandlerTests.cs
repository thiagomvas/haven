using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Sidecars.Commands.DisableSidecar;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Sidecars.Commands.DisableSidecar;

[Category("Unit")]
public sealed class DisableSidecarHandlerTests
{
    private ISidecarRepository _sidecarRepository;
    private IDeploymentJobEnqueuer _deploymentJobEnqueuer;
    private DisableSidecarHandler _sut;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _deploymentJobEnqueuer = Substitute.For<IDeploymentJobEnqueuer>();
        _sut = new DisableSidecarHandler(_sidecarRepository, _deploymentJobEnqueuer);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenSidecarDoesNotExist()
    {
        var command = new DisableSidecarCommand { SidecarId = Guid.NewGuid() };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>())
            .Returns((Sidecar?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnSuccessWithoutEnqueueing_WhenAlreadyDisabled()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        var command = new DisableSidecarCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _deploymentJobEnqueuer.DidNotReceive().EnqueueSidecarStop(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_ShouldDisableAndEnqueueStop_WhenEnabledAndRunning()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        sidecar.Enable();
        sidecar.MarkDeployed();
        var command = new DisableSidecarCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        sidecar.Enabled.ShouldBeFalse();
        sidecar.Status.ShouldBe(ServiceStatus.Stopped);
        _deploymentJobEnqueuer.Received(1).EnqueueSidecarStop(sidecar.Id);
    }

    [Test]
    public async Task Handle_ShouldDisableWithoutEnqueueing_WhenEnabledButAlreadyStopped()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        sidecar.Enable();
        var command = new DisableSidecarCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        sidecar.Enabled.ShouldBeFalse();
        _deploymentJobEnqueuer.DidNotReceive().EnqueueSidecarStop(Arg.Any<Guid>());
    }
}
