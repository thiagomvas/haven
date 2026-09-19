using Docker.DotNet.Models;

using Haven.Application.Common;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Infrastructure.Deployment.Docker;
using Haven.Infrastructure.Services;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Services;

[Category("Unit")]
public sealed class ContainerAndBashRunnerTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();
    private IDockerContainerRuntime _runtime = null!;

    [SetUp]
    public void Setup() => _runtime = Substitute.For<IDockerContainerRuntime>();

    private void Inspect(ContainerState state) =>
        _runtime.InspectByServiceIdAsync(ServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<ContainerInspectResponse>.Success(new ContainerInspectResponse { State = state }));

    private static HealthCheck Container() => HealthCheck.Create(ServiceId, "c", HealthCheckKind.Container, true, null, "");
    private static HealthCheck Bash(string config) => HealthCheck.Create(ServiceId, "b", HealthCheckKind.Bash, true, null, config);

    // ---- Container ----------------------------------------------------------------------------------------------

    [Test]
    public async Task Container_Missing_IsUnknownContainerNotFound()
    {
        _runtime.InspectByServiceIdAsync(ServiceId, Arg.Any<CancellationToken>())
            .Returns(Result<ContainerInspectResponse>.Failure(Error.Docker.ContainerNotFound));

        var result = await new ContainerHealthCheckRunner(_runtime).RunHealthCheckAsync(Container());

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerNotFound);
    }

    [Test]
    public async Task Container_Stopped_IsUnhealthyWithExitCode()
    {
        Inspect(new ContainerState { Running = false, Status = "exited", ExitCode = 137 });

        var result = await new ContainerHealthCheckRunner(_runtime).RunHealthCheckAsync(Container());

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerNotRunning);
        result.ExitCode.ShouldBe(137);
        result.Message.ShouldContain("exited");
    }

    [Test]
    public async Task Container_RunningWithoutDockerHealthcheck_IsHealthy()
    {
        Inspect(new ContainerState { Running = true });

        var result = await new ContainerHealthCheckRunner(_runtime).RunHealthCheckAsync(Container());

        result.Status.ShouldBe(ServiceHealth.Healthy);
    }

    [Test]
    public async Task Container_DockerUnhealthy_SurfacesLastProbeOutput()
    {
        Inspect(new ContainerState
        {
            Running = true,
            Health = new Health
            {
                Status = "unhealthy",
                FailingStreak = 3,
                Log = [new HealthcheckResult { ExitCode = 1, Output = "curl: (7) connection refused" }]
            }
        });

        var result = await new ContainerHealthCheckRunner(_runtime).RunHealthCheckAsync(Container());

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerUnhealthy);
        result.Output.ShouldBe("curl: (7) connection refused");
        result.Message.ShouldContain("3");
    }

    [Test]
    public async Task Container_DockerStarting_IsUnknown()
    {
        Inspect(new ContainerState { Running = true, Health = new Health { Status = "starting" } });

        var result = await new ContainerHealthCheckRunner(_runtime).RunHealthCheckAsync(Container());

        result.Status.ShouldBe(ServiceHealth.Unknown);
    }

    // ---- Bash ---------------------------------------------------------------------------------------------------

    private BashHealthCheckRunner NewBash() => new(_runtime, Substitute.For<ILogger<BashHealthCheckRunner>>());

    [Test]
    public async Task Bash_EmptyCommand_IsUnknownInvalidConfig()
    {
        var result = await NewBash().RunHealthCheckAsync(Bash("""{"command":""}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.InvalidConfig);
    }

    [Test]
    public async Task Bash_ExpectedExitCode_IsHealthy()
    {
        _runtime.ExecInContainerByServiceIdAsync(ServiceId, "true", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Result<(long ExitCode, string StdOut, string StdErr)>.Success((0, "fine", "")));

        var result = await NewBash().RunHealthCheckAsync(Bash("""{"command":"true"}"""));

        result.Status.ShouldBe(ServiceHealth.Healthy);
        result.Output.ShouldBe("fine");
    }

    [Test]
    public async Task Bash_WrongExitCode_IsUnhealthyWithOutputAndBothCodes()
    {
        _runtime.ExecInContainerByServiceIdAsync(ServiceId, "pg_isready", Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Result<(long ExitCode, string StdOut, string StdErr)>.Success((2, "no response", "socket error")));

        var result = await NewBash().RunHealthCheckAsync(Bash("""{"command":"pg_isready","expectedExitCode":0}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.UnexpectedExitCode);
        result.ExitCode.ShouldBe(2);
        result.Message.ShouldContain("2");
        result.Message.ShouldContain("0");
        result.Output.ShouldContain("no response");
        result.Output.ShouldContain("socket error");
    }

    [Test]
    public async Task Bash_ContainerMissing_IsUnknown()
    {
        _runtime.ExecInContainerByServiceIdAsync(ServiceId, Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Result<(long ExitCode, string StdOut, string StdErr)>.Failure(Error.Docker.ContainerNotFound));

        var result = await NewBash().RunHealthCheckAsync(Bash("""{"command":"true"}"""));

        result.Status.ShouldBe(ServiceHealth.Unknown);
        result.Reason.ShouldBe(HealthCheckFailureReason.ContainerNotFound);
    }

    [Test]
    public async Task Bash_Timeout_IsUnhealthyTimeout()
    {
        _runtime.ExecInContainerByServiceIdAsync(ServiceId, Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns<Result<(long ExitCode, string StdOut, string StdErr)>>(_ => throw new TaskCanceledException());

        var result = await NewBash().RunHealthCheckAsync(Bash("""{"command":"sleep 30","timeoutSeconds":1}"""));

        result.Status.ShouldBe(ServiceHealth.Unhealthy);
        result.Reason.ShouldBe(HealthCheckFailureReason.Timeout);
    }
}
