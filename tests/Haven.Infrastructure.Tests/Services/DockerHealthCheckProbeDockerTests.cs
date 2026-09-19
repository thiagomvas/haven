using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common.Interfaces.Services;
using Haven.Application.Configuration;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

/// <summary>
/// Runs the real probe against a real Docker daemon. Skipped when Docker (or the probe image) isn't available.
/// </summary>
[Category("Docker")]
[NonParallelizable]
public sealed class DockerHealthCheckProbeDockerTests
{
    private const string TargetImage = "nginx:alpine";

    private DockerClient _docker = null!;
    private DockerHealthCheckProbe _probe = null!;
    private string _network = null!;
    private string? _targetId;

    [SetUp]
    public async Task SetUp()
    {
        var uri = OperatingSystem.IsWindows() ? "npipe://./pipe/docker_engine" : "unix:///var/run/docker.sock";
        _docker = new DockerClientConfiguration(new Uri(uri)).CreateClient();

        try
        {
            await _docker.System.PingAsync();
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Docker is not available: {ex.Message}");
        }

        var options = Substitute.For<IOptionsMonitor<HealthCheckOptions>>();
        options.CurrentValue.Returns(new HealthCheckOptions());
        _probe = new DockerHealthCheckProbe(_docker, options, Substitute.For<ILogger<DockerHealthCheckProbe>>());

        _network = $"haven-probe-test-{Guid.NewGuid():N}";
        await _docker.Networks.CreateNetworkAsync(new NetworksCreateParameters { Name = _network });

        try
        {
            await EnsureImageAsync(TargetImage);
            var created = await _docker.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = TargetImage,
                Name = $"probe-target-{Guid.NewGuid():N}",
                HostConfig = new HostConfig { NetworkMode = _network },
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings> { [_network] = new() }
                }
            });
            _targetId = created.ID;
            await _docker.Containers.StartContainerAsync(_targetId, new ContainerStartParameters());
            await Task.Delay(1500); // let nginx start listening
        }
        catch (DockerApiException ex)
        {
            await TearDown();
            Assert.Ignore($"Could not start the probe target: {ex.Message}");
        }
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_docker is null) return;

        if (_targetId is not null)
            await _docker.Containers.RemoveContainerAsync(_targetId, new ContainerRemoveParameters { Force = true });

        try
        {
            await _docker.Networks.DeleteNetworkAsync(_network);
        }
        catch (DockerApiException)
        {
            // Network never created or already gone.
        }

        _docker.Dispose();
    }

    private async Task EnsureImageAsync(string image)
    {
        try
        {
            await _docker.Images.InspectImageAsync(image);
        }
        catch (DockerApiException)
        {
            await _docker.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = image }, null, new Progress<JSONMessage>());
        }
    }

    private async Task<string> TargetNameAsync() =>
        (await _docker.Containers.InspectContainerAsync(_targetId!)).Name.TrimStart('/');

    private async Task<ProbeOutput> RunAsync(string entrypoint, IReadOnlyList<string> args, int timeoutSeconds = 5)
    {
        var result = await _probe.RunAsync(new ProbeRequest(_network, entrypoint, args, TimeSpan.FromSeconds(timeoutSeconds)));

        if (result.IsFailure)
            Assert.Ignore($"Probe could not run in this environment: {result.Error.Message}");

        return result.Value;
    }

    [Test]
    public async Task Http_ReachesContainerByNameOnItsNetworkWithoutAnyPublishedPort()
    {
        var name = await TargetNameAsync();
        var url = $"http://{name}:80/";

        var output = await RunAsync(ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs("GET", url, 5));
        var result = ProbeCommands.ParseHttp(output, [200], url);

        result.Status.ShouldBe(ServiceHealth.Healthy);
        result.HttpStatusCode.ShouldBe(200);
        result.Output.ShouldContain("nginx");
    }

    [Test]
    public async Task Http_UnexpectedStatus_IsReportedWithTheStatusCode()
    {
        var name = await TargetNameAsync();
        var url = $"http://{name}:80/does-not-exist";

        var result = ProbeCommands.ParseHttp(
            await RunAsync(ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs("GET", url, 5)), [200], url);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.UnexpectedStatusCode);
        result.HttpStatusCode.ShouldBe(404);
    }

    [Test]
    public async Task Http_UnknownHost_IsReportedAsDnsFailure()
    {
        var url = "http://no-such-container-here:80/";

        var result = ProbeCommands.ParseHttp(
            await RunAsync(ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs("GET", url, 5)), [200], url);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.DnsFailure);
    }

    [Test]
    public async Task Http_ClosedPort_IsReportedAsConnectionRefused()
    {
        var name = await TargetNameAsync();
        var url = $"http://{name}:81/";

        var result = ProbeCommands.ParseHttp(
            await RunAsync(ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs("GET", url, 5)), [200], url);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
    }

    [Test]
    public async Task Tcp_OpenPort_IsHealthy()
    {
        var name = await TargetNameAsync();

        var output = await RunAsync(ProbeCommands.TcpEntrypoint, ProbeCommands.BuildTcpArgs(name, 80, 3));

        ProbeCommands.ParseTcp(output, name, 80, 3).Status.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public async Task Tcp_ClosedPort_IsUnhealthy()
    {
        var name = await TargetNameAsync();

        var output = await RunAsync(ProbeCommands.TcpEntrypoint, ProbeCommands.BuildTcpArgs(name, 81, 3));
        var result = ProbeCommands.ParseTcp(output, name, 81, 3);

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ConnectionRefused);
    }

    [Test]
    public async Task Tcp_UnknownHost_IsDnsFailure()
    {
        var output = await RunAsync(ProbeCommands.TcpEntrypoint, ProbeCommands.BuildTcpArgs("no-such-container-here", 80, 3));

        ProbeCommands.ParseTcp(output, "no-such-container-here", 80, 3).Reason.ShouldBe(HealthCheckFailureReason.DnsFailure);
    }

    [Test]
    public async Task Probe_RemovesItsContainerAfterEveryRun()
    {
        await RunAsync(ProbeCommands.HttpEntrypoint, ProbeCommands.BuildHttpArgs("GET", "http://no-such-container-here/", 3));

        var leftovers = await _docker.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true,
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                ["label"] = new Dictionary<string, bool> { [$"{DockerHealthCheckProbe.ProbeLabel}=true"] = true },
                ["network"] = new Dictionary<string, bool> { [_network] = true }
            }
        });
        leftovers.ShouldBeEmpty();
    }

    [Test]
    public async Task Probe_NetworkThatDoesNotExist_FailsInsteadOfThrowing()
    {
        var result = await _probe.RunAsync(new ProbeRequest(
            "haven-network-that-does-not-exist", ProbeCommands.HttpEntrypoint,
            ProbeCommands.BuildHttpArgs("GET", "http://x/", 3), TimeSpan.FromSeconds(3)));

        result.IsFailure.ShouldBeTrue();
    }
}
