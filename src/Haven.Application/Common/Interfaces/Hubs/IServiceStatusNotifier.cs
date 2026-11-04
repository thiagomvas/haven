using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Hubs;

/// <summary>Full observed state of a service, pushed to clients so they never need to infer fields from the status.</summary>
public sealed record ServiceStatusUpdate(
    Guid ServiceId,
    string ServiceName,
    ServiceDisplayStatus Status,
    DesiredState DesiredState,
    ServiceRuntimeState RuntimeState,
    ServiceHealth Health);

public interface IServiceStatusNotifier
{
    Task NotifyStatusChangedAsync(ServiceStatusUpdate update, CancellationToken cancellationToken = default);
}