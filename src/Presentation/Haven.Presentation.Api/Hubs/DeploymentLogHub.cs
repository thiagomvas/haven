using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Hubs;

public class DeploymentLogHub : Hub
{
    public async Task SubscribeToDeployment(string deploymentId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, $"deployment-{deploymentId}");

    public async Task UnsubscribeFromDeployment(string deploymentId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"deployment-{deploymentId}");
}
