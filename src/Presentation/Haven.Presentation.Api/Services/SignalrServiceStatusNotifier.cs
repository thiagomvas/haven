using Haven.Application.Common.Interfaces.Hubs;
using Haven.Presentation.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Services;

public class SignalrServiceStatusNotifier(IHubContext<ServiceStatusHub> hubContext, ILogger<SignalrServiceStatusNotifier> logger) : IServiceStatusNotifier
{
    public async Task NotifyStatusChangedAsync(ServiceStatusUpdate update, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"service-{update.ServiceId}")
            .SendAsync("ServiceStatusChanged", new
            {
                ServiceId = update.ServiceId,
                ServiceName = update.ServiceName,
                NewStatus = update.Status.ToString(),
                DesiredState = update.DesiredState.ToString(),
                RuntimeState = update.RuntimeState.ToString(),
                Health = update.Health.ToString()
            }, cancellationToken);
        logger.LogInformation("Notified clients about status change for service {ServiceId} to {NewStatus}", update.ServiceId, update.Status);
    }
}
