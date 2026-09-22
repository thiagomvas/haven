using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Common.Interfaces.Services;
using Haven.Domain.Aggregates;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

using Environment = Haven.Domain.Aggregates.Environment;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class TraefikRoutingHealerTests
{
    private ISidecarRepository _sidecarRepository = null!;
    private IServiceRegistryEntryRepository _serviceRegistryEntryRepository = null!;
    private ITraefikApiClient _traefikApiClient = null!;
    private IDockerContainerRuntime _containerRuntime = null!;
    private ILogger<TraefikRoutingHealer> _logger = null!;
    private TraefikRoutingHealer _sut = null!;

    private Sidecar _traefik = null!;
    private Service _service = null!;
    private ServiceRegistryEntry _entry = null!;

    [SetUp]
    public void Setup()
    {
        _sidecarRepository = Substitute.For<ISidecarRepository>();
        _serviceRegistryEntryRepository = Substitute.For<IServiceRegistryEntryRepository>();
        _traefikApiClient = Substitute.For<ITraefikApiClient>();
        _containerRuntime = Substitute.For<IDockerContainerRuntime>();
        _logger = Substitute.For<ILogger<TraefikRoutingHealer>>();
        _sut = new TraefikRoutingHealer(_sidecarRepository, _serviceRegistryEntryRepository, _traefikApiClient, _containerRuntime, _logger);

        var project = Project.Create("acme", alias: "acme");
        var environment = Environment.Create(project.Id, "prod", alias: "prod");
        _service = Service.Reconstitute(Guid.NewGuid(), environment.Id, "api", "api", ServiceType.DockerImage,
            ExposureMode.None, ServiceStatus.Running, DateTime.UtcNow, DateTime.UtcNow, environment: environment);

        _traefik = Sidecar.Create("traefik", SidecarKind.Traefik);
        _traefik.Enabled = true;
        _sidecarRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns([_traefik]);

        _entry = ServiceRegistryEntry.Create(_service.Id);
        _entry.ContainerName = "haven-acme-prod-api";
        _entry.AddDomain("api.example.com", 8080);
        _serviceRegistryEntryRepository.GetForServiceAsync(_service.Id, Arg.Any<CancellationToken>()).Returns(_entry);
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenExpectedIpIsNull_ReturnsFalseWithoutQueryingTraefik()
    {
        var result = await _sut.VerifyAndHealAsync(_service.Id, null, CancellationToken.None);

        result.ShouldBeFalse();
        await _traefikApiClient.DidNotReceive().GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenTraefikDisabled_ReturnsFalseWithoutQuerying()
    {
        _traefik.Enabled = false;

        var result = await _sut.VerifyAndHealAsync(_service.Id, "172.29.61.4", CancellationToken.None);

        result.ShouldBeFalse();
        await _traefikApiClient.DidNotReceive().GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenResolvedUrlMatchesExpectedIp_DoesNotRestart()
    {
        _traefikApiClient.GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(["http://172.29.61.4:8080"]));

        var result = await _sut.VerifyAndHealAsync(_service.Id, "172.29.61.4", CancellationToken.None);

        result.ShouldBeFalse();
        await _containerRuntime.DidNotReceive().RestartByServiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenTraefikUnreachable_DoesNotRestart()
    {
        _traefikApiClient.GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failed);

        var result = await _sut.VerifyAndHealAsync(_service.Id, "172.29.61.4", CancellationToken.None);

        result.ShouldBeFalse();
        await _containerRuntime.DidNotReceive().RestartByServiceIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenResolvedUrlStaysMismatchedAfterRetries_RestartsTraefik()
    {
        _traefikApiClient.GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(["http://172.17.0.2:8080"]));
        _containerRuntime.RestartByServiceIdAsync(_traefik.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var result = await _sut.VerifyAndHealAsync(_service.Id, "172.29.61.4", CancellationToken.None);

        result.ShouldBeTrue();
        await _containerRuntime.Received(1).RestartByServiceIdAsync(_traefik.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task VerifyAndHealAsync_WhenRestartFails_ReturnsFalse()
    {
        _traefikApiClient.GetServiceServerUrlsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(["http://172.17.0.2:8080"]));
        _containerRuntime.RestartByServiceIdAsync(_traefik.Id, Arg.Any<CancellationToken>())
            .Returns(Error.Docker.OperationFailed("boom"));

        var result = await _sut.VerifyAndHealAsync(_service.Id, "172.29.61.4", CancellationToken.None);

        result.ShouldBeFalse();
    }
}
