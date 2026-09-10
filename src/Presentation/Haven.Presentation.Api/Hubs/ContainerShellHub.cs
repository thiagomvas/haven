using Haven.Domain.Enums;
using Haven.Presentation.Api.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Hubs;

/// <summary>
/// Interactive container shells. Every method requires authentication,
/// and each shell session belongs to exactly one connection.
/// </summary>
[Authorize]
public class ContainerShellHub(ContainerShellSessionManager sessionManager) : Hub
{
    /// <summary>Starts a new interactive shell in the container backing <paramref name="serviceId"/> and returns its session id.</summary>
    public Task<Guid> StartShell(Guid projectId, Guid environmentId, Guid serviceId, ShellType shellType)
        => sessionManager.StartAsync(Context.ConnectionId, projectId, environmentId, serviceId, shellType, Context.ConnectionAborted);

    /// <summary>Writes raw bytes (keystrokes) to the shell's stdin.</summary>
    public Task SendInput(Guid sessionId, byte[] data)
        => sessionManager.SendInputAsync(sessionId, data, Context.ConnectionAborted);

    public Task StopShell(Guid sessionId)
        => sessionManager.StopAsync(sessionId, Context.ConnectionAborted);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await sessionManager.StopAllForConnectionAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
