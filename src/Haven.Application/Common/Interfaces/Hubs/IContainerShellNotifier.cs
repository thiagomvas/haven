namespace Haven.Application.Common.Interfaces.Hubs;

/// <summary>
/// Pushes interactive shell output/lifecycle events to the specific SignalR connection that owns
/// a shell session (not a broadcast group, each session belongs to exactly one client).
/// </summary>
public interface IContainerShellNotifier
{
    Task SendOutputAsync(string connectionId, Guid sessionId, byte[] data, CancellationToken cancellationToken = default);

    Task SendClosedAsync(string connectionId, Guid sessionId, string? reason, CancellationToken cancellationToken = default);
}
