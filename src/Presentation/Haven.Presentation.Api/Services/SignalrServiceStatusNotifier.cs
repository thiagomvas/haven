using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Presentation.Api.Hubs;

using Microsoft.AspNetCore.SignalR;

namespace Haven.Presentation.Api.Services;

public class SignalrServiceStatusNotifier(IHubContext<ServiceStatusHub> hubContext, ILogger<SignalrServiceStatusNotifier> logger) : IServiceStatusNotifier
{
    public async Task NotifyStatusChangedAsync(Guid serviceId, string serviceName, ServiceStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"service-{serviceId}")
            .SendAsync("ServiceStatusChanged", new
            {
                ServiceId = serviceId,
                ServiceName = serviceName,
                NewStatus = newStatus.ToString()
            }, cancellationToken);
        logger.LogInformation("Notified clients about status change for service {ServiceId} to {NewStatus}", serviceId, newStatus);
    }
}