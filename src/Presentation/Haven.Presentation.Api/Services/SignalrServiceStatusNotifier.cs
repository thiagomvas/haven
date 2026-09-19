using Haven.Application.Common.Interfaces.Hubs;
using Haven.Application.Features.Services;
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

    public async Task NotifyHealthChangedAsync(Guid serviceId, string serviceName, ServiceHealth health, ServiceHealthIssueDto? issue,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"service-{serviceId}")
            .SendAsync("ServiceHealthChanged", new
            {
                ServiceId = serviceId,
                ServiceName = serviceName,
                Health = health.ToString(),
                Issue = issue is null
                    ? null
                    : new { issue.CheckName, Reason = issue.Reason.ToString(), issue.Message }
            }, cancellationToken);
        logger.LogInformation("Notified clients about health change for service {ServiceId} to {Health}", serviceId, health);
    }
}
