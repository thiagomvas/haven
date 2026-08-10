using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Commands.DeleteNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Commands.DeleteNetwork;

[Category("Unit")]
public sealed class DeleteNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;
    private DeleteNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingService = Substitute.For<INetworkingService>();
        _networkingServiceFactory.Create(ServiceType.DockerImage).Returns(_networkingService);
        _networkingService.DisconnectServiceFromNetworksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _networkingService.DeleteNetworkAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _sut = new DeleteNetworkHandler(_networkRepository, _networkingServiceFactory);
    }

    [Test]
    public async Task Handle_WithUnknownNetwork_ReturnsNotFound()
    {
        _networkRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Network?)null);

        var result = await _sut.Handle(new DeleteNetworkCommand { NetworkId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithProjectEnvironmentNetwork_ReturnsInvalidOperation()
    {
        var network = Network.Create("haven-acme-prod", NetworkType.ProjectEnvironment, Guid.NewGuid(), Guid.NewGuid());
        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);

        var result = await _sut.Handle(new DeleteNetworkCommand { NetworkId = network.Id }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithSharedNetwork_DisconnectsServicesAndDeletes()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var serviceId = Guid.NewGuid();

        var populated = Network.Reconstitute(
            network.Id, network.Name, network.Type, network.Metadata,
            null, null, network.CreatedAt, network.UpdatedAt,
            serviceNetworks: [Haven.Domain.Entities.ServiceNetwork.Create(serviceId, network.Id)]);

        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(populated);

        var result = await _sut.Handle(new DeleteNetworkCommand { NetworkId = network.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _networkingService.Received(1).DisconnectServiceFromNetworksAsync(serviceId, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
        await _networkingService.Received(1).DeleteNetworkAsync(network.Id, Arg.Any<CancellationToken>());
        await _networkRepository.Received(1).DeleteAsync(network.Id, Arg.Any<CancellationToken>());
    }
}
