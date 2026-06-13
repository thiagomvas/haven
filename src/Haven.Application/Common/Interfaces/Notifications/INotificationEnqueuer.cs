using Haven.Domain.Entities;
using Haven.Domain.Events;

namespace Haven.Application.Common.Interfaces.Notifications;

public interface INotificationEnqueuer
{
    Task<Guid> EnqueueAsync(NotificationRule rule, DomainEvent domainEvent, CancellationToken ct = default);
}
