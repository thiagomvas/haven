using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Infrastructure.Deployment;
using Haven.Infrastructure.Utils;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Docker;

[Category("Unit")]
public sealed class DockerContainerRuntimeTests
{
    private DockerContainerRuntime _sut = null!;
    private ILogger<DockerContainerRuntime> _logger = null!;
    private IDockerClient _client = null!;
    private INetworkingService _networkingService = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DockerContainerRuntime>>();
        _client = Substitute.For<IDockerClient>();
        _networkingService = Substitute.For<INetworkingService>();

        _networkingService.ConnectServiceToNetworksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _networkingService.DisconnectServiceFromAllNetworksAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        _sut = new DockerContainerRuntime(_client, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
    }

    [Test]
    public void BuildContainerParameters_SetsNameLabelsAndImage()
    {
        var labels = new Dictionary<string, string> { { "haven.managed", "true" } };

        var param = _sut.BuildContainerParameters("my-container", labels, "my-image:latest", null, ExposureMode.None, [], []);

        param.Name.ShouldBe("my-container");
        param.Labels.ShouldBe(labels);
        param.Image.ShouldBe("my-image:latest");
    }

    [Test]
    public void BuildContainerParameters_WithEnvVars_SetsEnv()
    {
        var envs = new List<EnvironmentVariables> { new() { Key = "FOO", Value = "bar" } };

        var param = _sut.BuildContainerParameters("name", new Dictionary<string, string>(), "image", envs, ExposureMode.None, [], []);

        param.Env.ShouldContain("FOO=bar");
    }

    [Test]
    public void BuildContainerParameters_ExposureModeInternal_AddsListenAddressEnvVar()
    {
        var param = _sut.BuildContainerParameters("name", new Dictionary<string, string>(), "image", null, ExposureMode.Internal, [], []);

        param.Env.ShouldContain("LISTEN_ADDRESS=127.0.0.1");
    }

    [Test]
    public void BuildContainerParameters_ExposureModeNone_DoesNotAddListenAddress()
    {
        var param = _sut.BuildContainerParameters("name", new Dictionary<string, string>(), "image", null, ExposureMode.None, [], []);

        (param.Env ?? []).ShouldNotContain(e => e.StartsWith("LISTEN_ADDRESS"));
    }

    [Test]
    public void BuildContainerParameters_WithPorts_SetsExposedPortsAndBindings()
    {
        var param = _sut.BuildContainerParameters("name", new Dictionary<string, string>(), "image", null, ExposureMode.Internal, ["8080:80"], []);

        param.ExposedPorts.ShouldContainKey("80/tcp");
        param.HostConfig.PortBindings.ShouldContainKey("80/tcp");
    }

    [Test]
    public void BuildContainerParameters_WithMounts_SetsHostConfigMounts()
    {
        var mounts = new List<Mount> { new() { Type = "bind", Source = "/host", Target = "/container" } };

        var param = _sut.BuildContainerParameters("name", new Dictionary<string, string>(), "image", null, ExposureMode.None, [], mounts);

        param.HostConfig.Mounts.ShouldBe(mounts);
    }

    [Test]
    public async Task CreateAndStartAsync_WhenStartSucceeds_ReturnsContainerId()
    {
        _client.Containers.CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "container-1" });
        _client.Containers.StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.CreateAndStartAsync(new CreateContainerParameters(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("container-1");
    }

    [Test]
    public async Task CreateAndStartAsync_WhenStartFails_ReturnsFailedToStartContainerError()
    {
        _client.Containers.CreateContainerAsync(Arg.Any<CreateContainerParameters>(), Arg.Any<CancellationToken>())
            .Returns(new CreateContainerResponse { ID = "container-1" });
        _client.Containers.StartContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStartParameters>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _sut.CreateAndStartAsync(new CreateContainerParameters(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.Docker.FailedToStartContainer);
    }

    [Test]
    public async Task ConnectToNetworksAsync_WhenNetworkIdsProvided_CallsConnectServiceToNetworksAsync()
    {
        var ownerId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        await _sut.ConnectToNetworksAsync(ownerId, [networkId], _networkingService, CancellationToken.None);

        await _networkingService.Received(1).ConnectServiceToNetworksAsync(ownerId, Arg.Is<IEnumerable<Guid>>(ids => ids.Contains(networkId)), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConnectToNetworksAsync_WhenNoNetworkIds_DoesNotCallNetworkingService()
    {
        await _sut.ConnectToNetworksAsync(Guid.NewGuid(), [], _networkingService, CancellationToken.None);

        await _networkingService.DidNotReceive().ConnectServiceToNetworksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ConnectToNetworksAsync_WhenFails_DoesNotThrow()
    {
        _networkingService.ConnectServiceToNetworksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Failed));

        await Should.NotThrowAsync(() => _sut.ConnectToNetworksAsync(Guid.NewGuid(), [Guid.NewGuid()], _networkingService, CancellationToken.None));
    }

    [Test]
    public async Task GetContainersByLabelAsync_BuildsExpectedLabelFilter()
    {
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        var label = DockerUtils.BuildIdLabel(Guid.NewGuid());
        await _sut.GetContainersByLabelAsync(label, CancellationToken.None);

        await _client.Containers.Received(1).ListContainersAsync(
            Arg.Is<ContainersListParameters>(p => p.All == true && p.Filters!["label"].ContainsKey($"{label.Key}={label.Value}")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopAndRemoveAsync_DisconnectsFromAllNetworksFirst()
    {
        var ownerId = Guid.NewGuid();

        await _sut.StopAndRemoveAsync([], ownerId, _networkingService, "reason", CancellationToken.None);

        await _networkingService.Received(1).DisconnectServiceFromAllNetworksAsync(ownerId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopAndRemoveAsync_WhenContainerRunning_StopsThenRemoves()
    {
        var container = new ContainerListResponse { ID = "c1", State = "running" };

        await _sut.StopAndRemoveAsync([container], Guid.NewGuid(), _networkingService, "reason", CancellationToken.None);

        await _client.Containers.Received(1).StopContainerAsync("c1", Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync("c1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopAndRemoveAsync_WhenContainerNotRunning_SkipsStopButRemoves()
    {
        var container = new ContainerListResponse { ID = "c1", State = "exited" };

        await _sut.StopAndRemoveAsync([container], Guid.NewGuid(), _networkingService, "reason", CancellationToken.None);

        await _client.Containers.DidNotReceive().StopContainerAsync(Arg.Any<string>(), Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync("c1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopAndRemoveAsync_WhenStopTimesOut_SwallowsAndRemovesAnyway()
    {
        var container = new ContainerListResponse { ID = "c1", State = "running" };
        _client.Containers.StopContainerAsync("c1", Arg.Any<ContainerStopParameters>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new TaskCanceledException());

        await Should.NotThrowAsync(() => _sut.StopAndRemoveAsync([container], Guid.NewGuid(), _networkingService, "reason", CancellationToken.None));

        await _client.Containers.Received(1).RemoveContainerAsync("c1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveAllForOwnerAsync_WhenNoContainersFound_DoesNotDisconnectOrRemove()
    {
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse>());

        await _sut.RemoveAllForOwnerAsync(Guid.NewGuid(), _networkingService, "reason", CancellationToken.None);

        await _networkingService.DidNotReceive().DisconnectServiceFromAllNetworksAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RemoveAllForOwnerAsync_WhenContainersFound_StopsAndRemovesThem()
    {
        var ownerId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "c1", State = "running" };
        _client.Containers.ListContainersAsync(Arg.Any<ContainersListParameters>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContainerListResponse> { container });

        await _sut.RemoveAllForOwnerAsync(ownerId, _networkingService, "reason", CancellationToken.None);

        await _networkingService.Received(1).DisconnectServiceFromAllNetworksAsync(ownerId, Arg.Any<CancellationToken>());
        await _client.Containers.Received(1).RemoveContainerAsync("c1", Arg.Any<ContainerRemoveParameters>(), Arg.Any<CancellationToken>());
    }
}