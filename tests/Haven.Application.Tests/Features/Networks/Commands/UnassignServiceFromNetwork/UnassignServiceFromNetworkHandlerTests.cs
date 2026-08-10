using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.UnassignServiceFromNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Commands.UnassignServiceFromNetwork;

[Category("Unit")]
public sealed class UnassignServiceFromNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;
    private UnassignServiceFromNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingService = Substitute.For<INetworkingService>();
        _networkingServiceFactory.Create(ServiceType.DockerImage).Returns(_networkingService);
        _sut = new UnassignServiceFromNetworkHandler(_networkRepository, _serviceRepository, _networkingServiceFactory);
    }

    [Test]
    public async Task Handle_WithValidNetworkAndService_DisconnectsService()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var service = Service.Create(Guid.NewGuid(), "api", ServiceType.DockerImage, ExposureMode.None);

        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);
        _serviceRepository.GetByIdAsync(service.Id, Arg.Any<CancellationToken>()).Returns(service);
        _networkingService.DisconnectServiceFromNetworksAsync(service.Id, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.Handle(
            new UnassignServiceFromNetworkCommand { NetworkId = network.Id, ServiceId = service.Id },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _networkingService.Received(1).DisconnectServiceFromNetworksAsync(
            service.Id,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == network.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_WithUnknownService_ReturnsNotFound()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);
        _serviceRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Service?)null);

        var result = await _sut.Handle(
            new UnassignServiceFromNetworkCommand { NetworkId = network.Id, ServiceId = Guid.NewGuid() },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}
