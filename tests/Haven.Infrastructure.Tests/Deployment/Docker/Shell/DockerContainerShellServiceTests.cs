using Docker.DotNet;
using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Deployment.Docker.Shell;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Docker.Shell;

[Category("Unit")]
public sealed class DockerContainerShellServiceTests
{
    private DockerContainerShellService _sut = null!;
    private ILogger<DockerContainerShellService> _logger = null!;
    private IDockerClient _client = null!;
    private IExecOperations _execOperations = null!;
    private IDockerContainerRuntime _containerRuntime = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<DockerContainerShellService>>();
        _client = Substitute.For<IDockerClient>();
        _execOperations = Substitute.For<IExecOperations>();
        _client.Exec.Returns(_execOperations);
        _containerRuntime = Substitute.For<IDockerContainerRuntime>();

        _sut = new DockerContainerShellService(_client, _containerRuntime, _logger);
    }

    [TearDown]
    public void TearDown() => _client.Dispose();

    [Test]
    public async Task CreateSessionAsync_ShouldReturnContainerNotFound_WhenNoContainerExistsForService()
    {
        var serviceId = Guid.NewGuid();
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[]);

        var result = await _sut.CreateSessionAsync(serviceId, ShellType.Bash, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(Error.Docker.ContainerNotFound);
    }

    [Test]
    public async Task CreateSessionAsync_ShouldReturnOperationFailed_WhenContainerIsNotRunning()
    {
        var serviceId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "container-1", State = "exited" };
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[container]);

        var result = await _sut.CreateSessionAsync(serviceId, ShellType.Bash, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe(Error.Docker.OperationFailed("x").Code);
    }

    [Test]
    public async Task CreateSessionAsync_ShouldNotCreateExec_WhenContainerIsNotRunning()
    {
        var serviceId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "container-1", State = "exited" };
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[container]);

        await _sut.CreateSessionAsync(serviceId, ShellType.Bash, CancellationToken.None);

        await _execOperations.DidNotReceive().ExecCreateContainerAsync(
            Arg.Any<string>(), Arg.Any<ContainerExecCreateParameters>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task CreateSessionAsync_ShouldReturnSuccess_WhenContainerIsRunning()
    {
        var serviceId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "container-1", State = "running" };
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[container]);

        _execOperations.ExecCreateContainerAsync(
                Arg.Any<string>(), Arg.Any<ContainerExecCreateParameters>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerExecCreateResponse { ID = "exec-1" });

        var stream = new MultiplexedStream(new MemoryStream(), multiplexed: true);
        _execOperations.StartAndAttachContainerExecAsync("exec-1", true, Arg.Any<CancellationToken>())
            .Returns(stream);

        var result = await _sut.CreateSessionAsync(serviceId, ShellType.Bash, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ServiceId.ShouldBe(serviceId);
        result.Value.ShellType.ShouldBe(ShellType.Bash);
    }

    [Test]
    public async Task CreateSessionAsync_ShouldExecOnFirstMatchingContainer_WhenMultipleContainersMatchLabel()
    {
        var serviceId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "container-1", State = "running" };
        var other = new ContainerListResponse { ID = "container-2", State = "running" };
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[container, other]);

        _execOperations.ExecCreateContainerAsync(
                Arg.Any<string>(), Arg.Any<ContainerExecCreateParameters>(), Arg.Any<CancellationToken>())
            .Returns(new ContainerExecCreateResponse { ID = "exec-1" });
        _execOperations.StartAndAttachContainerExecAsync("exec-1", true, Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(new MemoryStream(), multiplexed: true));

        await _sut.CreateSessionAsync(serviceId, ShellType.Bash, CancellationToken.None);

        await _execOperations.Received(1).ExecCreateContainerAsync(
            "container-1", Arg.Any<ContainerExecCreateParameters>(), Arg.Any<CancellationToken>());
    }

    [TestCase(ShellType.Bash, "/bin/bash")]
    [TestCase(ShellType.Sh, "/bin/sh")]
    public async Task CreateSessionAsync_ShouldExecTheShellBinaryMatchingShellType(ShellType shellType, string expectedCommand)
    {
        var serviceId = Guid.NewGuid();
        var container = new ContainerListResponse { ID = "container-1", State = "running" };
        _containerRuntime.GetContainersByLabelAsync(Arg.Any<KeyValuePair<string, string>>(), Arg.Any<CancellationToken>())
            .Returns((IList<ContainerListResponse>)[container]);

        ContainerExecCreateParameters? capturedParameters = null;
        _execOperations.ExecCreateContainerAsync(
                Arg.Any<string>(), Arg.Do<ContainerExecCreateParameters>(p => capturedParameters = p), Arg.Any<CancellationToken>())
            .Returns(new ContainerExecCreateResponse { ID = "exec-1" });
        _execOperations.StartAndAttachContainerExecAsync("exec-1", true, Arg.Any<CancellationToken>())
            .Returns(new MultiplexedStream(new MemoryStream(), multiplexed: true));

        await _sut.CreateSessionAsync(serviceId, shellType, CancellationToken.None);

        capturedParameters.ShouldNotBeNull();
        capturedParameters!.Cmd.ShouldBe([expectedCommand]);
        capturedParameters.AttachStdin.ShouldBeTrue();
        capturedParameters.AttachStdout.ShouldBeTrue();
        capturedParameters.AttachStderr.ShouldBeTrue();
        capturedParameters.Tty.ShouldBeTrue();
    }
}
