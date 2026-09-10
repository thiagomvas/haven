using Haven.Application.Common.Interfaces.Hubs;
using Haven.Presentation.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Services;

public class SignalrContainerShellNotifier(IHubContext<ContainerShellHub> hubContext) : IContainerShellNotifier
{
    public Task SendOutputAsync(string connectionId, Guid sessionId, byte[] data, CancellationToken cancellationToken = default)
        => hubContext.Clients.Client(connectionId).SendAsync("ShellOutput", sessionId, data, cancellationToken);

    public Task SendClosedAsync(string connectionId, Guid sessionId, string? reason, CancellationToken cancellationToken = default)
        => hubContext.Clients.Client(connectionId).SendAsync("ShellClosed", sessionId, reason, cancellationToken);
}
