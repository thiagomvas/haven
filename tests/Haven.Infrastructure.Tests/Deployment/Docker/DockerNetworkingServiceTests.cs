using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Persistence;
using Haven.Testing.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using Network = Haven.Domain.Aggregates.Network;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class DockerNetworkingServiceTests
{
    private IDockerClient _client = null!;
    private HavenDbContext _db = null!;
    private ILogger<DockerNetworkingService> _logger = null!;
    private DockerNetworkingService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _logger = Substitute.For<ILogger<DockerNetworkingService>>();
        _sut = new DockerNetworkingService(_db, _logger, _client);

        // No container exists for the service in any of these tests.
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _client.Dispose();
    }

    private (Project Project, Service Service) SeedServiceWithoutContainer()
    {
        var project = Project.Create("acme", alias: "acme");
        var environment = project.AddEnvironment("prod", alias: "prod");
        var service = project.AddService(environment.Id, "api", ServiceType.DockerImage, ExposureMode.None, alias: "api");
        _db.Projects.Add(project);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return (project, service);
    }

    [Test]
    public async Task ConnectServiceToNetworksAsync_WhenNoContainerExists_StillPersistsMembership()
    {
        var (_, service) = SeedServiceWithoutContainer();
        var network = Network.Create("shared-net", NetworkType.Shared);
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        await _db.SaveChangesAsync();

        var result = await _sut.ConnectServiceToNetworksAsync(service.Id, [network.Id], CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var serviceNetwork = await _db.ServiceNetworks.FindAsync(service.Id, network.Id);
        serviceNetwork.ShouldNotBeNull();

        await _client.Networks.DidNotReceive().ConnectNetworkAsync(
            Arg.Any<string>(), Arg.Any<NetworkConnectParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisconnectServiceFromNetworksAsync_WhenNoContainerExists_StillRemovesMembership()
    {
        var (_, service) = SeedServiceWithoutContainer();
        var network = Network.Create("shared-net", NetworkType.Shared);
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        _db.ServiceNetworks.Add(ServiceNetwork.Create(service.Id, network.Id));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var result = await _sut.DisconnectServiceFromNetworksAsync(service.Id, [network.Id], CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var serviceNetwork = await _db.ServiceNetworks.FindAsync(service.Id, network.Id);
        serviceNetwork.ShouldBeNull();
    }

    [Test]
    public async Task ConnectServiceToNetworksAsync_WhenContainerExists_PerformsLiveConnect()
    {
        var (_, service) = SeedServiceWithoutContainer();
        var network = Network.Create("shared-net", NetworkType.Shared);
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        await _db.SaveChangesAsync();

        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns([new ContainerListResponse { ID = "container-1" }]);
        _client.Networks.InspectNetworkAsync("docker-net-1", Arg.Any<CancellationToken>())
            .Returns(new NetworkResponse());

        var result = await _sut.ConnectServiceToNetworksAsync(service.Id, [network.Id], CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _client.Networks.Received(1).ConnectNetworkAsync(
            "docker-net-1",
            Arg.Is<NetworkConnectParameters>(p => p.Container == "container-1"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DisconnectServiceFromAllNetworksAsync_PreservesSharedAndExternalMembership_OnlyRemovesProjectEnvironment()
    {
        var (project, service) = SeedServiceWithoutContainer();
        var environment = project.Environments.Single();

        var envNetwork = Network.CreateProjectEnvironmentNetwork(project.Id, "acme", environment.Id, "prod");
        envNetwork.SetDockerNetworkId("docker-net-env");
        var sharedNetwork = Network.Create("shared-net", NetworkType.Shared);
        sharedNetwork.SetDockerNetworkId("docker-net-shared");
        var externalNetwork = Network.Create("external-net", NetworkType.External);
        externalNetwork.SetDockerNetworkId("docker-net-external");

        _db.Networks.AddRange(envNetwork, sharedNetwork, externalNetwork);
        _db.ServiceNetworks.AddRange(
            ServiceNetwork.Create(service.Id, envNetwork.Id),
            ServiceNetwork.Create(service.Id, sharedNetwork.Id),
            ServiceNetwork.Create(service.Id, externalNetwork.Id));
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        // Container is running, so the live Docker disconnect path also runs - harmless/best-effort.
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns([new ContainerListResponse { ID = "container-1" }]);

        var result = await _sut.DisconnectServiceFromAllNetworksAsync(service.Id, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var remaining = await _db.ServiceNetworks
            .Where(sn => sn.ServiceId == service.Id)
            .Select(sn => sn.NetworkId)
            .ToListAsync();

        remaining.ShouldNotContain(envNetwork.Id);
        remaining.ShouldContain(sharedNetwork.Id);
        remaining.ShouldContain(externalNetwork.Id);
    }
}