using Haven.Domain;

namespace Haven.Application.Common.Interfaces.Hubs;

public interface IServiceStatusNotifier
{
    Task NotifyStatusChangedAsync(Guid serviceId, string serviceName, ServiceStatus newStatus, CancellationToken cancellationToken = default);
}