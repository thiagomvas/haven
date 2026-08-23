using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Queries.SearchAttachableServices;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Networks.Queries.SearchAttachableServices;

[Category("Unit")]
public sealed class SearchAttachableServicesHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private IServiceRepository _serviceRepository = null!;
    private SearchAttachableServicesHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _serviceRepository = Substitute.For<IServiceRepository>();
        _sut = new SearchAttachableServicesHandler(_networkRepository, _serviceRepository);
    }

    [Test]
    public async Task Handle_WithUnknownNetwork_ReturnsNotFound()
    {
        _networkRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Network?)null);

        var result = await _sut.Handle(
            new SearchAttachableServicesQuery { NetworkId = Guid.NewGuid(), Search = "api" },
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_WithValidNetwork_ReturnsResultsFromRepository()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var expected = new List<AttachableServiceDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "api",
                Status = "Running",
                ProjectId = Guid.NewGuid(),
                ProjectName = "Storefront",
                EnvironmentId = Guid.NewGuid(),
                EnvironmentName = "prod"
            }
        };

        _networkRepository.GetByIdAsync(network.Id, Arg.Any<CancellationToken>()).Returns(network);
        _serviceRepository
            .SearchAttachableAsync(network.Id, "api", 20, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new SearchAttachableServicesQuery { NetworkId = network.Id, Search = "api" },
            CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected);
    }
}
