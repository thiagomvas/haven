using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Networks.Queries.ListNetworks;
using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

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
    public async Task Handle_PassesTypeFilterToRepository()
    {
        var query = new ListNetworksQuery { Type = NetworkType.Shared };
        _networkRepository.GetAllAsync(NetworkType.Shared, Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.Handle(query, CancellationToken.None);

        await _networkRepository.Received(1)
            .GetAllAsync(NetworkType.Shared, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_MapsNetworksToDtos()
    {
        var network = Network.Create("shared-network", NetworkType.Shared);
        var query = new ListNetworksQuery();

        _networkRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns([network]);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Value.Single().Id.ShouldBe(network.Id);
        result.Value.Single().Name.ShouldBe("shared-network");
        result.Value.Single().Type.ShouldBe(nameof(NetworkType.Shared));
    }

    [Test]
    public async Task Handle_FlattensScopeAndConnectedServices()
    {
        var project = Project.Create("acme");
        var environment = Environment.Create(project.Id, "production", "prod", "acme");
        var service = Service.Create(environment.Id, "api", ServiceType.DockerImage, ExposureMode.None);
        service.Environment = environment;
        environment.Project = project;

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
            serviceNetworks: [ServiceNetwork.Reconstitute(service.Id, Guid.NewGuid(), service, null, "172.16.5.3")],
            subnet: "172.16.5.0/24",
            gateway: "172.16.5.1");

        var query = new ListNetworksQuery();
        _networkRepository.GetAllAsync(null, Arg.Any<CancellationToken>())
            .Returns([network]);

        var result = await _sut.Handle(query, CancellationToken.None);

        var dto = result.Value.Single();
        dto.ProjectName.ShouldBe("acme");
        dto.EnvironmentName.ShouldBe("production");
        dto.Subnet.ShouldBe("172.16.5.0/24");
        dto.Gateway.ShouldBe("172.16.5.1");
        dto.ServiceCount.ShouldBe(1);
        dto.Services.Single().Name.ShouldBe("api");
        dto.Services.Single().Status.ShouldBe(service.Status.ToString());
        dto.Services.Single().IpAddress.ShouldBe("172.16.5.3");
        dto.Services.Single().ProjectName.ShouldBe("acme");
    }
}
