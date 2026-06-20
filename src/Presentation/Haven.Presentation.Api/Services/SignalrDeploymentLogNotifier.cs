using Haven.Application.Common.Interfaces.Hubs;
using Haven.Presentation.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Services;

public class SignalrDeploymentLogNotifier(IHubContext<DeploymentLogHub> hubContext) : IDeploymentLogNotifier
{
    public Task SendLogEntryAsync(Guid deploymentId, string message, CancellationToken ct = default)
        => hubContext.Clients
            .Group($"deployment-{deploymentId}")
            .SendAsync("ReceiveLogEntry", new { Message = message, Timestamp = DateTime.UtcNow }, ct);
}