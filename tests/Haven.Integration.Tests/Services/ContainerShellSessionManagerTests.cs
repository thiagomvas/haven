using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Application.Features.Services.Commands.OpenShellSession;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;
using Haven.Presentation.Api.Services;

using Mediator;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Integration.Tests.Services;

[Category("Unit")]
public sealed class ContainerShellSessionManagerTests
{
    private IMediator _mediator = null!;
    private IServiceScopeFactory _scopeFactory = null!;
    private IContainerShellNotifier _notifier = null!;
    private ContainerShellSessionManager _sut = null!;

    [SetUp]
    public void Setup()
    {
        _mediator = Substitute.For<IMediator>();

        var services = new ServiceCollection();
        services.AddSingleton(_mediator);
        var provider = services.BuildServiceProvider();
        _scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        _notifier = Substitute.For<IContainerShellNotifier>();
        var logger = Substitute.For<ILogger<ContainerShellSessionManager>>();

        _sut = new ContainerShellSessionManager(_scopeFactory, _notifier, logger);
    }

    private static IShellSession CreateSession(Guid? sessionId = null, Guid? serviceId = null)
    {
        var session = Substitute.For<IShellSession>();
        session.SessionId.Returns(sessionId ?? Guid.NewGuid());
        session.ServiceId.Returns(serviceId ?? Guid.NewGuid());
        session.ShellType.Returns(ShellType.Bash);
        return session;
    }

    /// <summary>Blocks ReadAsync until <paramref name="cancellationToken"/> is cancelled, then throws, mimicking a session that is stopped explicitly rather than exiting on its own.</summary>
    private static void NeverCompletesUntilCancelled(IShellSession session)
    {
        session.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                Task.Delay(Timeout.Infinite, callInfo.ArgAt<CancellationToken>(1))
                    .ContinueWith<ShellReadResult>(_ => throw new OperationCanceledException(), TaskScheduler.Default));
    }

    [Test]
    public async Task StartAsync_ShouldReturnSessionId_WhenMediatorSucceeds()
    {
        var session = CreateSession();
        NeverCompletesUntilCancelled(session);
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));

        var sessionId = await _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        sessionId.ShouldBe(session.SessionId);

        await _sut.StopAsync(sessionId, CancellationToken.None);
    }

    [Test]
    public async Task StartAsync_ShouldSendCommandWithGivenParameters()
    {
        var projectId = Guid.NewGuid();
        var environmentId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var session = CreateSession();
        NeverCompletesUntilCancelled(session);
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));

        var sessionId = await _sut.StartAsync("conn-1", projectId, environmentId, serviceId, ShellType.Sh, CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<OpenShellSessionCommand>(c =>
                c.ProjectId == projectId && c.EnvironmentId == environmentId && c.ServiceId == serviceId && c.ShellType == ShellType.Sh),
            Arg.Any<CancellationToken>());

        await _sut.StopAsync(sessionId, CancellationToken.None);
    }

    [Test]
    public void StartAsync_ShouldThrowHubException_WhenMediatorResultIsFailure()
    {
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Failure(Error.Docker.ContainerNotFound));

        Should.ThrowAsync<HubException>(() =>
            _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None));
    }

    [Test]
    public void StartAsync_ShouldThrowHubException_WhenMediatorThrowsHavenException()
    {
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Result<IShellSession>>>(_ => throw new NotFoundException("Project", Guid.NewGuid()));

        Should.ThrowAsync<HubException>(() =>
            _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None));
    }

    [Test]
    public void SendInputAsync_ShouldThrowHubException_WhenSessionNotFound()
        => Should.ThrowAsync<HubException>(() => _sut.SendInputAsync(Guid.NewGuid(), [1, 2, 3], CancellationToken.None));

    [Test]
    public async Task SendInputAsync_ShouldWriteToSession_WhenSessionExists()
    {
        var session = CreateSession();
        NeverCompletesUntilCancelled(session);
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));
        var sessionId = await _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        byte[] data = [1, 2, 3];
        await _sut.SendInputAsync(sessionId, data, CancellationToken.None);

        await session.Received(1).WriteAsync(data, Arg.Any<CancellationToken>());

        await _sut.StopAsync(sessionId, CancellationToken.None);
    }

    [Test]
    public async Task StopAsync_ShouldBeNoOp_WhenSessionNotFound()
        => await _sut.StopAsync(Guid.NewGuid(), CancellationToken.None);

    [Test]
    public async Task StopAsync_ShouldDisposeSessionAndNotifyClosed()
    {
        var session = CreateSession();
        NeverCompletesUntilCancelled(session);
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));
        var sessionId = await _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        await _sut.StopAsync(sessionId, CancellationToken.None);

        await session.Received(1).DisposeAsync();
        await _notifier.Received(1).SendClosedAsync("conn-1", sessionId, null, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StopAsync_ShouldRemoveSession_SoASecondSendInputFails()
    {
        var session = CreateSession();
        NeverCompletesUntilCancelled(session);
        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));
        var sessionId = await _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        await _sut.StopAsync(sessionId, CancellationToken.None);

        await Should.ThrowAsync<HubException>(() => _sut.SendInputAsync(sessionId, [1], CancellationToken.None));
    }

    [Test]
    public async Task PumpOutputAsync_ShouldForwardOutputToNotifier_UntilEof()
    {
        var session = CreateSession();
        session.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(
                callInfo =>
                {
                    "hi"u8.ToArray().CopyTo(callInfo.ArgAt<Memory<byte>>(0));
                    return Task.FromResult(new ShellReadResult(2, false));
                },
                _ => Task.FromResult(new ShellReadResult(0, true)));

        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(session));

        var sessionId = await _sut.StartAsync("conn-1", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        // The output pump runs on a background task started by StartAsync; give it a moment to
        // drain the two queued ReadAsync results (data, then EOF) before asserting.
        await Task.Delay(200);

        await _notifier.Received(1).SendOutputAsync(
            "conn-1", sessionId, Arg.Is<byte[]>(b => b.Length == 2 && b[0] == (byte)'h' && b[1] == (byte)'i'), Arg.Any<CancellationToken>());
        await _notifier.Received(1).SendClosedAsync("conn-1", sessionId, null, Arg.Any<CancellationToken>());
        await session.Received(1).DisposeAsync();
    }

    [Test]
    public async Task StopAllForConnectionAsync_ShouldStopOnlySessionsOwnedByThatConnection()
    {
        var sessionA = CreateSession();
        NeverCompletesUntilCancelled(sessionA);
        var sessionB = CreateSession();
        NeverCompletesUntilCancelled(sessionB);

        _mediator.Send(Arg.Any<OpenShellSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result<IShellSession>.Success(sessionA), Result<IShellSession>.Success(sessionB));

        var sessionAId = await _sut.StartAsync("conn-a", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);
        var sessionBId = await _sut.StartAsync("conn-b", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ShellType.Bash, CancellationToken.None);

        await _sut.StopAllForConnectionAsync("conn-a");

        await sessionA.Received(1).DisposeAsync();
        await sessionB.DidNotReceive().DisposeAsync();

        await _sut.StopAsync(sessionBId, CancellationToken.None);
    }
}
