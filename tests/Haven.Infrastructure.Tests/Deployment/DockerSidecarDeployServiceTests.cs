using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common.Interfaces;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment;

[Category("Unit")]
public sealed class DockerSidecarDeployServiceTests
{
    private DockerSidecarDeployService _sut = null!;
    private IDockerClient _client = null!;
    private IDockerContainerRuntime _containerRuntime = null!;
    private INetworkRepository _networkRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;

    [SetUp]
    public void Setup()
    {
        var logger = Substitute.For<ILogger<DockerSidecarDeployService>>();
        _client = Substitute.For<IDockerClient>();

        _client.Containers
            .ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        _client.Images
            .CreateImageAsync(Arg.Any<ImagesCreateParameters>(), Arg.Any<AuthConfig>(), Arg.Any<IProgress<JSONMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _client.Containers
            .CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "test-container-id" });

        _client.Containers
            .StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _client.Containers
            .InspectContainerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerInspectResponse
            {
                NetworkSettings = new NetworkSettings { Networks = new Dictionary<string, EndpointSettings>() },
                HostConfig = new HostConfig { PortBindings = new Dictionary<string, IList<PortBinding>>() }
            });

        _containerRuntime = new DockerContainerRuntime(_client, Substitute.For<ILogger<DockerContainerRuntime>>());

        _networkRepository = Substitute.For<INetworkRepository>();
        _networkRepository.GetAllAsync(Arg.Any<NetworkType?>(), Arg.Any<CancellationToken>())
            .Returns(new List<Haven.Domain.Aggregates.Network>());

        _networkingService = Substitute.For<INetworkingService>();
        _networkingService.ConnectServiceToNetworksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Haven.Application.Common.Result.Success());
        _networkingService.EnsureNetworkExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Haven.Application.Common.Result.Success());

        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingServiceFactory.Create(ServiceType.DockerImage)
            .Returns(_networkingService);

        _sut = new DockerSidecarDeployService(logger, _client, _containerRuntime, _networkRepository, _networkingServiceFactory);
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task DeployAsync_ForTraefikSidecar_ShouldMountDockerSocketReadWrite()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik,
            sourceConfig: new DockerConfig { Image = "traefik:v3.0" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.HostConfig!.Mounts != null &&
                p.HostConfig.Mounts.Any(m =>
                    m.Type == "bind" && m.Source == "/var/run/docker.sock" &&
                    m.Target == "/var/run/docker.sock" && m.ReadOnly != true)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_ForTraefikSidecarWithoutAcme_ShouldNotMountAcmeVolume()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik,
            sourceConfig: new DockerConfig { Image = "traefik:v3.0" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.HostConfig!.Mounts != null &&
                p.HostConfig.Mounts.All(m => m.Target != "/letsencrypt")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_ForTraefikSidecarWithAcme_ShouldMountAcmeVolume()
    {
        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik,
            sourceConfig: new DockerConfig
            {
                Image = "traefik:v3.0",
                CommandArgs = ["--certificatesresolvers.letsencrypt.acme.httpchallenge=true"]
            });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.HostConfig!.Mounts != null &&
                p.HostConfig.Mounts.Any(m =>
                    m.Type == "volume" && m.Source == "haven-traefik-acme" && m.Target == "/letsencrypt")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_ForNonTraefikSidecar_ShouldNotMountDockerSocket()
    {
        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami,
            sourceConfig: new DockerConfig { Image = "traefik/whoami" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p => p.HostConfig!.Mounts == null || p.HostConfig.Mounts.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WhenSystemNetworkExists_ShouldAttachToItAtCreationTime()
    {
        var systemNetwork = Haven.Domain.Aggregates.Network.CreateSystemNetwork();
        systemNetwork.SetDockerNetworkId("docker-system-network-id");
        _networkRepository.GetAllAsync(NetworkType.System, Arg.Any<CancellationToken>())
            .Returns(new List<Haven.Domain.Aggregates.Network> { systemNetwork });

        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami,
            sourceConfig: new DockerConfig { Image = "traefik/whoami" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p =>
                p.NetworkingConfig != null &&
                p.NetworkingConfig.EndpointsConfig.ContainsKey("docker-system-network-id")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_WhenNoSystemNetworkExists_ShouldNotSetNetworkingConfig()
    {
        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami,
            sourceConfig: new DockerConfig { Image = "traefik/whoami" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _client.Containers.Received(1).CreateContainerAsync(
            Arg.Is<CreateContainerParameters>(p => p.NetworkingConfig == null),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_ForTraefikSidecar_ShouldConnectToEveryProjectEnvironmentNetwork()
    {
        var envNetwork = Haven.Domain.Aggregates.Network.CreateProjectEnvironmentNetwork(
            Guid.NewGuid(), "proj", Guid.NewGuid(), "dev");
        _networkRepository.GetAllAsync(NetworkType.ProjectEnvironment, Arg.Any<CancellationToken>())
            .Returns(new List<Haven.Domain.Aggregates.Network> { envNetwork });

        var sidecar = Sidecar.Create("traefik", SidecarKind.Traefik,
            sourceConfig: new DockerConfig { Image = "traefik:v3.0" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _networkingService.Received(1).ConnectServiceToNetworksAsync(
            sidecar.Id,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(envNetwork.Id)),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeployAsync_ForNonTraefikSidecar_ShouldNotConnectToProjectEnvironmentNetworks()
    {
        var envNetwork = Haven.Domain.Aggregates.Network.CreateProjectEnvironmentNetwork(
            Guid.NewGuid(), "proj", Guid.NewGuid(), "dev");
        _networkRepository.GetAllAsync(NetworkType.ProjectEnvironment, Arg.Any<CancellationToken>())
            .Returns(new List<Haven.Domain.Aggregates.Network> { envNetwork });

        var sidecar = Sidecar.Create("whoami", SidecarKind.Whoami,
            sourceConfig: new DockerConfig { Image = "traefik/whoami" });

        await _sut.DeployAsync(sidecar, null, CancellationToken.None);

        await _networkingService.DidNotReceive().ConnectServiceToNetworksAsync(
            sidecar.Id,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(envNetwork.Id)),
            Arg.Any<CancellationToken>());
    }
}
