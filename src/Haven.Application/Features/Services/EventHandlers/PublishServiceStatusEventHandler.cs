using Haven.Application.Common.Interfaces.Hubs;
using Haven.Domain.Enums;
using Haven.Domain.Events;

using Mediator;

namespace Haven.Application.Features.Services.EventHandlers;

/// <summary>
/// Single place that turns any event affecting a service's displayed state into a push to clients.
/// The pushed state is always recomputed from the database (deployment, registry entry, desired state),
/// never inferred from the event payload, so event ordering cannot produce a stale or contradictory status.
/// </summary>
public sealed class PublishServiceStatusEventHandler(IServiceStatusPublisher publisher)
    : INotificationHandler<ServiceDeployingEvent>,
      INotificationHandler<ServiceDeployedEvent>,
      INotificationHandler<ServiceRestartedEvent>,
      INotificationHandler<ServiceStoppedEvent>,
      INotificationHandler<ServiceRuntimeStateChangedEvent>,
      INotificationHandler<ServiceHealthChangedEvent>
{
    public ValueTask Handle(ServiceDeployingEvent n, CancellationToken ct) => PublishAsync(n.ServiceId, ct);
    public ValueTask Handle(ServiceDeployedEvent n, CancellationToken ct) => PublishAsync(n.ServiceId, ct);
    public ValueTask Handle(ServiceRestartedEvent n, CancellationToken ct) => PublishAsync(n.ServiceId, ct);
    public ValueTask Handle(ServiceStoppedEvent n, CancellationToken ct) => PublishAsync(n.ServiceId, ct);

    public ValueTask Handle(ServiceRuntimeStateChangedEvent n, CancellationToken ct) =>
        n.OwnerKind == OwnerKind.Service ? PublishAsync(n.OwnerId, ct) : ValueTask.CompletedTask;

    public ValueTask Handle(ServiceHealthChangedEvent n, CancellationToken ct) =>
        n.OwnerKind == OwnerKind.Service ? PublishAsync(n.OwnerId, ct) : ValueTask.CompletedTask;

    private async ValueTask PublishAsync(Guid serviceId, CancellationToken ct) =>
        await publisher.PublishAsync(serviceId, ct);
}