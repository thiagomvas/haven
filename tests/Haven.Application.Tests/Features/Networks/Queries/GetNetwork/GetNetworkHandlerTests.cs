using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Queries.GetNetwork;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Queries.GetNetwork;

[Category("Unit")]
public sealed class GetNetworkHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private GetNetworkHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _sut = new GetNetworkHandler(_networkRepository);
    }

    [Test]
    public async Task Handle_WithExistingNetwork_ReturnsDto()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);

        var result = await _sut.Handle(new GetNetworkQuery { NetworkId = network.Id }, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(network.Id);
        result.Value.Name.ShouldBe("shared-network");
    }

    [Test]
    public async Task Handle_WithUnknownNetwork_ReturnsNotFound()
    {
        _networkRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Network?)null);

        var result = await _sut.Handle(new GetNetworkQuery { NetworkId = Guid.NewGuid() }, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }
}