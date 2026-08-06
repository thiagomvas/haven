using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Messaging;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Application.Tests.Features.Networks.Queries.ListNetworks;

[Category("Unit")]
public sealed class ListNetworksHandlerTests
{
    private INetworkRepository _networkRepository = null!;
    private ListNetworksHandler _sut = null!;

    [SetUp]
    public void Setup()
    {
        _networkRepository = Substitute.For<INetworkRepository>();
        _sut = new ListNetworksHandler(_networkRepository);
    }

    [Test]
    public async Task Handle_PassesPagingAndTypeFilterToRepository()
    {
        var query = new ListNetworksQuery { PageNumber = 2, PageSize = 10, Type = NetworkType.Shared };
        _networkRepository.GetPagedAsync(2, 10, NetworkType.Shared, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Network>([], 0, 2, 10));

        await _sut.Handle(query, CancellationToken.None);

        await _networkRepository.Received(1)
            .GetPagedAsync(2, 10, NetworkType.Shared, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_MapsNetworksToDtos()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var query = new ListNetworksQuery { PageNumber = 1, PageSize = 20 };

        _networkRepository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Network>([network], 1, 1, 20));

        var result = await _sut.Handle(query, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(network.Id);
        result.Items.Single().Name.ShouldBe("shared-network");
        result.Items.Single().Type.ShouldBe(nameof(NetworkType.Shared));
    }

    [Test]
    public async Task Handle_FlattensScopeAndConnectedServices()
    {
        var project = Project.Create("acme");
        var environment = Environment.Create(project.Id, "production", "prod", "acme");
        var service = Service.Create(environment.Id, "api", ServiceType.DockerImage, ExposureMode.None);

        var network = Network.Reconstitute(
            Guid.NewGuid(),
            "haven-acme-production",
            NetworkType.ProjectEnvironment,
            metadata: null,
            projectId: project.Id,
            environmentId: environment.Id,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow,
            project: project,
            environment: environment,
            serviceNetworks: [ServiceNetwork.Reconstitute(service.Id, Guid.NewGuid(), service, null)]);

        var query = new ListNetworksQuery { PageNumber = 1, PageSize = 20 };
        _networkRepository.GetPagedAsync(1, 20, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Network>([network], 1, 1, 20));

        var result = await _sut.Handle(query, CancellationToken.None);

        var dto = result.Items.Single();
        dto.ProjectName.ShouldBe("acme");
        dto.EnvironmentName.ShouldBe("production");
        dto.ServiceCount.ShouldBe(1);
        dto.Services.Single().Name.ShouldBe("api");
        dto.Services.Single().Status.ShouldBe(service.Status.ToString());
    }
}
