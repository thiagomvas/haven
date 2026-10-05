using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Application.Features.CustomActions.Services;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

namespace Haven.Application.Tests.Features.CustomActions;

[Category("Unit")]
public sealed class ExecActionStrategyTests
{
    private IContainerExecService _exec = null!;
    private ExecActionStrategy _sut = null!;

    [SetUp]
    public void Setup()
    {
        _exec = Substitute.For<IContainerExecService>();
        _sut = new ExecActionStrategy(_exec, NullLogger<ExecActionStrategy>.Instance);
    }

    private void SetupExec(Result<ContainerExecResult> result) =>
        _exec.ExecAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(result);

    private static ExecActionConfig Config(string[]? command = null, string? workingDir = null, string? user = null,
        ShellType? shell = null) => new(command ?? ["echo", "hi"], workingDir, user, shell);

    [Test]
    public void CanHandle_ExecConfig_ReturnsTrue() =>
        _sut.CanHandle(CustomActionTestData.Create(Config())).ShouldBeTrue();

    [Test]
    public void CanHandle_HttpConfig_ReturnsFalse() =>
        _sut.CanHandle(CustomActionTestData.Create(CustomActionTestData.Http())).ShouldBeFalse();

    [Test]
    public async Task ExecuteAsync_NonExecConfig_ReturnsNotSupported()
    {
        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(CustomActionTestData.Http()),
            CancellationToken.None);

        result.Error.ShouldBe(Error.NotSupported);
        await _exec.DidNotReceiveWithAnyArgs().ExecAsync(default, default!, default, default, default, default);
    }

    [Test]
    public async Task ExecuteAsync_EmptyCommand_ReturnsValidationError()
    {
        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config(command: [])), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("VALIDATION");
        await _exec.DidNotReceiveWithAnyArgs().ExecAsync(default, default!, default, default, default, default);
    }

    [Test]
    public async Task ExecuteAsync_ExitCodeZero_ReturnsSuccess()
    {
        SetupExec(new ContainerExecResult(0, "ok", ""));

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task ExecuteAsync_NoShell_PassesCommandAsArgvWithOptions()
    {
        SetupExec(new ContainerExecResult(0, "", ""));
        var serviceId = Guid.NewGuid();
        var action = CustomActionTestData.Create(Config(["ls", "-la"], "/app", "root"), TimeSpan.FromSeconds(7),
            serviceId);

        await _sut.ExecuteAsync(action, CancellationToken.None);

        await _exec.Received(1).ExecAsync(serviceId,
            Arg.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { "ls", "-la" })), "/app", "root",
            TimeSpan.FromSeconds(7), Arg.Any<CancellationToken>());
    }

    [TestCase(ShellType.Bash, "/bin/bash")]
    [TestCase(ShellType.Sh, "/bin/sh")]
    public async Task ExecuteAsync_WithShell_WrapsJoinedCommandInShell(ShellType shell, string path)
    {
        SetupExec(new ContainerExecResult(0, "", ""));
        var action = CustomActionTestData.Create(Config(["echo", "a", "&&", "echo", "b"], shell: shell));

        await _sut.ExecuteAsync(action, CancellationToken.None);

        await _exec.Received(1).ExecAsync(action.ServiceId,
            Arg.Is<IReadOnlyList<string>>(c => c.SequenceEqual(new[] { path, "-c", "echo a && echo b" })),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ExecuteAsync_NonZeroExitCode_ReturnsFailureWithCodeAndStdErr()
    {
        SetupExec(new ContainerExecResult(2, "", " no such file \n"));

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("code 2");
        result.Error.Message.ShouldContain("no such file");
    }

    [Test]
    public async Task ExecuteAsync_NonZeroExitCodeWithoutStdErr_ReturnsFailureWithCodeOnly()
    {
        SetupExec(new ContainerExecResult(1, "", "  "));

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), CancellationToken.None);

        result.Error.Message.ShouldBe("Command exited with code 1.");
    }

    [Test]
    public async Task ExecuteAsync_ExecServiceFails_PropagatesError()
    {
        SetupExec(Error.Docker.ContainerNotFound);

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), CancellationToken.None);

        result.Error.ShouldBe(Error.Docker.ContainerNotFound);
    }

    [Test]
    public async Task ExecuteAsync_CallerCancels_ReturnsCancelledOperation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _exec.ExecAsync(default, default!, default, default, default, default).ThrowsAsyncForAnyArgs(
            new OperationCanceledException(cts.Token));

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), cts.Token);

        result.Error.ShouldBe(Error.CancelledOperation);
    }

    [Test]
    public async Task ExecuteAsync_TimeoutCancellation_ReturnsTimeoutFailure()
    {
        _exec.ExecAsync(default, default!, default, default, default, default)
            .ThrowsAsyncForAnyArgs(new OperationCanceledException());

        var result = await _sut.ExecuteAsync(CustomActionTestData.Create(Config()), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldContain("timed out");
    }
}