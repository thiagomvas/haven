using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Hubs;

public class ServiceStatusHub : Hub
{
    public async Task SubscribeToService(string serviceId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"service-{serviceId}");
    }

    public async Task UnsubscribeFromService(string serviceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"service-{serviceId}");
    }
}