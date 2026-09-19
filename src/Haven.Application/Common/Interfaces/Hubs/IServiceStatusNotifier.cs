using Haven.Application.Features.Services;
using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Hubs;

public interface IServiceStatusNotifier
{
    Task NotifyStatusChangedAsync(Guid serviceId, string serviceName, ServiceStatus newStatus, CancellationToken cancellationToken = default);

    Task NotifyHealthChangedAsync(Guid serviceId, string serviceName, ServiceHealth health, ServiceHealthIssueDto? issue, CancellationToken cancellationToken = default);
}