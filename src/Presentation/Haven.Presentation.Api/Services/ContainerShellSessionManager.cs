using System.Collections.Concurrent;

using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Common.Interfaces.Shell;
using Haven.Domain.Enums;

using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Services;

/// <summary>
/// Tracks active interactive shell sessions across SignalR connections and pumps each session's
/// output to its owning connection via <see cref="IContainerShellNotifier"/>. Registered as a
/// singleton so sessions survive across the short-lived <see cref="Hubs.ContainerShellHub"/>
/// instances SignalR creates per invocation.
/// </summary>
public class ContainerShellSessionManager
{
    private const int ReadBufferSize = 4096;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContainerShellNotifier _notifier;
    private readonly ILogger<ContainerShellSessionManager> _logger;
    private readonly ConcurrentDictionary<Guid, ActiveSession> _sessions = new();

    public ContainerShellSessionManager(
        IServiceScopeFactory scopeFactory,
        IContainerShellNotifier notifier,
        ILogger<ContainerShellSessionManager> logger)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task<Guid> StartAsync(string connectionId, Guid serviceId, ShellType shellType, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var shellService = scope.ServiceProvider.GetRequiredService<IContainerShellService>();

        var result = await shellService.CreateSessionAsync(serviceId, shellType, cancellationToken);
        if (result.IsFailure)
            throw new HubException(result.Error.Message);

        var session = result.Value;
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var active = new ActiveSession(session, connectionId, cts);
        active.PumpTask = PumpOutputAsync(active);

        _sessions[session.SessionId] = active;
        return session.SessionId;
    }

    public Task SendInputAsync(Guid sessionId, byte[] data, CancellationToken cancellationToken)
        => _sessions.TryGetValue(sessionId, out var active)
            ? active.Session.WriteAsync(data, cancellationToken)
            : throw new HubException("Shell session not found.");

    public async Task StopAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (!_sessions.TryGetValue(sessionId, out var active))
            return;

        await active.Cts.CancelAsync();
        await active.PumpTask;
    }

    public async Task StopAllForConnectionAsync(string connectionId)
    {
        var owned = _sessions.Values.Where(a => a.ConnectionId == connectionId).ToList();

        foreach (var active in owned)
            await active.Cts.CancelAsync();

        await Task.WhenAll(owned.Select(a => a.PumpTask));
    }

    /// <summary>
    /// Reads until EOF or cancellation, then always removes, disposes and notifies exactly once.
    /// The single place a session's lifecycle ends, whether it exits on its own, is stopped
    /// explicitly, or the connection drops.
    /// </summary>
    private async Task PumpOutputAsync(ActiveSession active)
    {
        var buffer = new byte[ReadBufferSize];
        string? closeReason = null;

        try
        {
            while (!active.Cts.IsCancellationRequested)
            {
                var result = await active.Session.ReadAsync(buffer, active.Cts.Token);
                if (result.Eof)
                    break;

                await _notifier.SendOutputAsync(active.ConnectionId, active.Session.SessionId, buffer[..result.Count], active.Cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Session was stopped explicitly (StopAsync/disconnect).
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shell session '{SessionId}' output pump failed", active.Session.SessionId);
            closeReason = "The shell session ended unexpectedly.";
        }
        finally
        {
            _sessions.TryRemove(active.Session.SessionId, out _);
            await active.Session.DisposeAsync();
            active.Cts.Dispose();

            try
            {
                await _notifier.SendClosedAsync(active.ConnectionId, active.Session.SessionId, closeReason);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to notify connection '{ConnectionId}' that shell session '{SessionId}' closed", active.ConnectionId, active.Session.SessionId);
            }
        }
    }

    private sealed class ActiveSession(IShellSession session, string connectionId, CancellationTokenSource cts)
    {
        public IShellSession Session { get; } = session;
        public string ConnectionId { get; } = connectionId;
        public CancellationTokenSource Cts { get; } = cts;
        public Task PumpTask { get; set; } = Task.CompletedTask;
    }
}
