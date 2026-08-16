using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Sidecars.Commands.EnableSidecar;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Sidecars.Commands.EnableSidecar;

[Category("Unit")]
public sealed class EnableSidecarHandlerTests
{
    private ISidecarRepository _sidecarRepository;
    private IDeploymentJobEnqueuer _deploymentJobEnqueuer;
    private IHavenEnvironment _havenEnvironment;
    private EnableSidecarHandler _sut;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _deploymentJobEnqueuer = Substitute.For<IDeploymentJobEnqueuer>();
        _havenEnvironment = Substitute.For<IHavenEnvironment>();
        _havenEnvironment.IsDevelopment.Returns(true);
        _sut = new EnableSidecarHandler(_sidecarRepository, _deploymentJobEnqueuer, _havenEnvironment);
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenSidecarDoesNotExist()
    {
        var command = new EnableSidecarCommand { SidecarId = Guid.NewGuid() };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>())
            .Returns((Sidecar?)null);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_ShouldReturnFailure_WhenWhoamiEnabledOutsideDevelopment()
    {
        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami);
        var command = new EnableSidecarCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);
        _havenEnvironment.IsDevelopment.Returns(false);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        sidecar.Enabled.ShouldBeFalse();
        _deploymentJobEnqueuer.DidNotReceive().EnqueueSidecarDeployment(Arg.Any<Guid>());
    }

    [Test]
    public async Task Handle_ShouldEnableAndEnqueueDeployment_WhenSidecarExists()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik);
        var command = new EnableSidecarCommand { SidecarId = sidecar.Id };
        _sidecarRepository.GetByIdAsync(command.SidecarId, Arg.Any<CancellationToken>()).Returns(sidecar);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        sidecar.Enabled.ShouldBeTrue();
        sidecar.Status.ShouldBe(ServiceStatus.DeploymentPending);
        _deploymentJobEnqueuer.Received(1).EnqueueSidecarDeployment(sidecar.Id);
    }
}
