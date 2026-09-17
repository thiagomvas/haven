using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;
using Network = Haven.Domain.Aggregates.Network;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class TraefikLabelMergerTests
{
    private ISidecarRepository _sidecarRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;
    private INetworkRepository _networkRepository = null!;
    private INetworkingServiceFactory _networkingServiceFactory = null!;
    private INetworkingService _networkingService = null!;
    private ILogger<TraefikLabelMerger> _logger = null!;
    private TraefikLabelMerger _sut = null!;

    private Sidecar _traefik = null!;
    private Service _service = null!;
    private Network _network = null!;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _networkRepository = Substitute.For<INetworkRepository>();
        _networkingServiceFactory = Substitute.For<INetworkingServiceFactory>();
        _networkingService = Substitute.For<INetworkingService>();
        _networkingServiceFactory.Create(ServiceType.DockerImage).Returns(_networkingService);
        _logger = Substitute.For<ILogger<TraefikLabelMerger>>();
        _sut = new TraefikLabelMerger(_sidecarRepository, _serviceRegistryEntryRepository, _networkRepository, _networkingServiceFactory, _logger);

        var project = Project.Create("acme", alias: "acme");
        var environment = Environment.Create(project.Id, "prod", alias: "prod");
        _service = Service.Reconstitute(Guid.NewGuid(), environment.Id, "api", "api", ServiceType.DockerImage,
            ExposureMode.None, ServiceStatus.Running, DateTime.UtcNow, DateTime.UtcNow, environment: environment);

        _traefik = Sidecar.Create("traefik", SidecarKind.Traefik);
        _traefik.Enabled = true;
        _sidecarRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_traefik]);

        _network = Network.CreateProjectEnvironmentNetwork(project.Id, project.Name, environment.Id, environment.Name);
        _networkRepository.GetByProjectAndEnvironmentAsync(project.Id, environment.Id, Arg.Any<CancellationToken>())
            .Returns([_network]);
    }

    private void SeedDomain()
    {
        var entry = ServiceRegistryEntry.Create(_service.Id);
        entry.AddDomain("api.example.com", 8080);
        _serviceRegistryEntryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(entry);
    }

    [Test]
    public async Task MergeAsync_WhenServiceHasDomainsAndEnvironmentNetwork_ConnectsTraefikToNetwork()
    {
        SeedDomain();
        _networkingService.ConnectServiceToNetworksAsync(_traefik.Id, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        await _sut.MergeAsync(_service, "haven-acme-prod-api", [], CancellationToken.None);

        await _networkingService.Received(1).ConnectServiceToNetworksAsync(
            _traefik.Id,
            Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == _network.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MergeAsync_WhenServiceHasNoDomains_DoesNotAttemptConnect()
    {
        _serviceRegistryEntryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>())
            .Returns((ServiceRegistryEntry?)null);

        await _sut.MergeAsync(_service, "haven-acme-prod-api", [], CancellationToken.None);

        await _networkingService.DidNotReceive().ConnectServiceToNetworksAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MergeAsync_WhenTraefikDisabled_DoesNotAttemptConnect()
    {
        _traefik.Enabled = false;
        SeedDomain();

        await _sut.MergeAsync(_service, "haven-acme-prod-api", [], CancellationToken.None);

        await _networkingService.DidNotReceive().ConnectServiceToNetworksAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MergeAsync_WhenConnectFails_LogsWarningButDoesNotThrow()
    {
        SeedDomain();
        _networkingService.ConnectServiceToNetworksAsync(_traefik.Id, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Error.Docker.OperationFailed("boom"));

        var labels = new Dictionary<string, string>();
        await Should.NotThrowAsync(() => _sut.MergeAsync(_service, "haven-acme-prod-api", labels, CancellationToken.None));

        labels["traefik.docker.network"].ShouldBe(_network.Name);
    }

    [Test]
    public async Task MergeAsync_WhenNoEnvironmentNetworkExists_DoesNotAttemptConnect()
    {
        SeedDomain();
        _networkRepository.GetByProjectAndEnvironmentAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.MergeAsync(_service, "haven-acme-prod-api", [], CancellationToken.None);

        await _networkingService.DidNotReceive().ConnectServiceToNetworksAsync(
            Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task MergeAsync_StillSetsTraefikDockerNetworkLabel_RegardlessOfConnectOutcome()
    {
        SeedDomain();
        _networkingService.ConnectServiceToNetworksAsync(_traefik.Id, Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Error.Docker.OperationFailed("boom"));

        var labels = new Dictionary<string, string>();
        await _sut.MergeAsync(_service, "haven-acme-prod-api", labels, CancellationToken.None);

        labels["traefik.docker.network"].ShouldBe(_network.Name);
    }
}
