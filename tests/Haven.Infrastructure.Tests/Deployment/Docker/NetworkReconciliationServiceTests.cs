using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Domain;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Persistence;
using Haven.Infrastructure.Utils;
using Haven.Testing.Common;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using Network = Haven.Domain.Aggregates.Network;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class NetworkReconciliationServiceTests
{
    private IDockerClient _client = null!;
    private HavenDbContext _db = null!;
    private ILogger<NetworkReconciliationService> _logger = null!;
    private NetworkReconciliationService _sut = null!;

    [SetUp]
    public void Setup()
    {
        _client = Substitute.For<IDockerClient>();
        _db = TestDbContextFactory.CreateUnitDbContext();
        _logger = Substitute.For<ILogger<NetworkReconciliationService>>();
        _sut = new NetworkReconciliationService(_db, _client, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
        _client.Dispose();
    }

    private (Guid ProjectId, Guid EnvironmentId) SeedProjectEnvironment()
    {
        var project = Project.Create("acme", alias: "acme");
        var environment = project.AddEnvironment("prod", alias: "prod");
        _db.Projects.Add(project);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return (project.Id, environment.Id);
    }

    [Test]
    public async Task ReconcileAsync_BackfillsSubnetAndGatewayFromDockerIpam()
    {
        var (projectId, environmentId) = SeedProjectEnvironment();
        var network = Network.CreateProjectEnvironmentNetwork(projectId, "acme", environmentId, "prod");
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        await _db.SaveChangesAsync();

        _client.Networks.InspectNetworkAsync("docker-net-1", Arg.Any<CancellationToken>())
            .Returns(new NetworkResponse
            {
                IPAM = new IPAM { Config = [new IPAMConfig { Subnet = "172.16.5.0/24", Gateway = "172.16.5.1" }] }
            });

        await _sut.ReconcileAsync(CancellationToken.None);

        var updated = await _db.Networks.FindAsync(network.Id);
        updated!.Subnet.ShouldBe("172.16.5.0/24");
        updated.Gateway.ShouldBe("172.16.5.1");
    }

    [Test]
    public async Task ReconcileAsync_SkipsNetworksAlreadyPopulated()
    {
        var (projectId, environmentId) = SeedProjectEnvironment();
        var network = Network.CreateProjectEnvironmentNetwork(projectId, "acme", environmentId, "prod");
        network.SetDockerNetworkId("docker-net-1");
        network.AssignNetworkInfo("10.0.0.0/24", "10.0.0.1");
        _db.Networks.Add(network);
        await _db.SaveChangesAsync();

        await _sut.ReconcileAsync(CancellationToken.None);

        await _client.Networks.DidNotReceive().InspectNetworkAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ReconcileAsync_WhenDockerNetworkMissing_LeavesSubnetNull()
    {
        var (projectId, environmentId) = SeedProjectEnvironment();
        var network = Network.CreateProjectEnvironmentNetwork(projectId, "acme", environmentId, "prod");
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        await _db.SaveChangesAsync();

        _client.Networks.InspectNetworkAsync("docker-net-1", Arg.Any<CancellationToken>())
            .Returns<Task<NetworkResponse>>(_ => throw new DockerApiException(System.Net.HttpStatusCode.NotFound, "not found"));

        await Should.NotThrowAsync(() => _sut.ReconcileAsync(CancellationToken.None));

        var updated = await _db.Networks.FindAsync(network.Id);
        updated!.Subnet.ShouldBeNull();
    }

    [Test]
    public async Task ReconcileAsync_BackfillsServiceIpAddress()
    {
        var project = Project.Create("acme", alias: "acme");
        var environment = project.AddEnvironment("prod", alias: "prod");
        var service = project.AddService(environment.Id, "api", ServiceType.DockerImage, ExposureMode.None, alias: "api");
        _db.Projects.Add(project);

        var network = Network.CreateProjectEnvironmentNetwork(project.Id, "acme", environment.Id, "prod");
        network.SetDockerNetworkId("docker-net-1");
        _db.Networks.Add(network);
        _db.ServiceNetworks.Add(ServiceNetwork.Create(service.Id, network.Id));
        await _db.SaveChangesAsync();

        var label = DockerUtils.BuildIdLabel(service.Id);
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns([new ContainerListResponse { ID = "container-1" }]);

        _client.Networks.InspectNetworkAsync("docker-net-1", Arg.Any<CancellationToken>())
            .Returns(new NetworkResponse
            {
                Containers = new Dictionary<string, EndpointResource>
                {
                    { "container-1", new EndpointResource { IPv4Address = "172.16.5.3/24" } }
                }
            });

        await _sut.ReconcileAsync(CancellationToken.None);

        var updated = await _db.ServiceNetworks.FindAsync(service.Id, network.Id);
        updated!.IpAddress.ShouldBe("172.16.5.3");
    }

    [Test]
    public async Task ReconcileAsync_WhenServiceNetworkExistsButContainerNotAttached_ReconnectsIt()
    {
        var project = Project.Create("acme", alias: "acme");
        var environment = project.AddEnvironment("prod", alias: "prod");
        var service = project.AddService(environment.Id, "api", ServiceType.DockerImage, ExposureMode.None, alias: "api");
        _db.Projects.Add(project);

        var network = Network.Create("shared-net", NetworkType.Shared);
        network.SetDockerNetworkId("docker-net-1");
        network.AssignNetworkInfo("172.16.5.0/24", "172.16.5.1"); // already populated - skips subnet reconciliation's InspectNetworkAsync call
        _db.Networks.Add(network);
        _db.ServiceNetworks.Add(ServiceNetwork.Create(service.Id, network.Id));
        await _db.SaveChangesAsync();

        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns([new ContainerListResponse { ID = "container-1" }]);

        // First inspect: container is not yet attached (e.g. crash-restarted before the initial
        // connect ever completed). Second inspect (after reconnecting): it is.
        _client.Networks.InspectNetworkAsync("docker-net-1", Arg.Any<CancellationToken>())
            .Returns(
                new NetworkResponse { Containers = new Dictionary<string, EndpointResource>() },
                new NetworkResponse
                {
                    Containers = new Dictionary<string, EndpointResource>
                    {
                        { "container-1", new EndpointResource { IPv4Address = "172.16.5.9/24" } }
                    }
                });

        await _sut.ReconcileAsync(CancellationToken.None);

        await _client.Networks.Received(1).ConnectNetworkAsync(
            "docker-net-1",
            Arg.Is<NetworkConnectParameters>(p => p.Container == "container-1"),
            Arg.Any<CancellationToken>());

        var updated = await _db.ServiceNetworks.FindAsync(service.Id, network.Id);
        updated!.IpAddress.ShouldBe("172.16.5.9");
    }
}