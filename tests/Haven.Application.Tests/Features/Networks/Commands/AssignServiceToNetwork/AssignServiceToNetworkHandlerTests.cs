using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.AssignServiceToNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Commands.AssignServiceToNetwork;

[Category("Unit")]
public sealed class AssignServiceToNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;
    private AssignServiceToNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingService = Substitute.For<INetworkingService>();
        _networkingServiceFactory.Create(ServiceType.DockerImage).Returns(_networkingService);
        _sut = new AssignServiceToNetworkHandler(_networkRepository, _serviceRepository, _networkingServiceFactory);
    }

    [Test]
    public async Task Handle_WithUnknownNetwork_ReturnsNotFound()
    {
        _networkRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Network?)null);

        var result = await _sut.Handle(
            new AssignServiceToNetworkCommand { NetworkId = Guid.NewGuid(), ServiceId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithUnknownService_ReturnsNotFound()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(
            new AssignServiceToNetworkCommand { NetworkId = network.Id, ServiceId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithValidNetworkAndService_ConnectsService()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var service = Service.Create(Guid.NewGuid(), "api", ServiceType.DockerImage, ExposureMode.None);

        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _networkingService.ConnectServiceToNetworksAsync(service.Id, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(
            new AssignServiceToNetworkCommand { NetworkId = network.Id, ServiceId = service.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _networkingService.Received(1).ConnectServiceToNetworksAsync(
            service.Id,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == network.Id),
            Arg.Any<CancellationToken>());
    }
}
