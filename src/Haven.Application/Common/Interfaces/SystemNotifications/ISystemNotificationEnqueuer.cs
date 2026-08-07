using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.SystemNotifications;

/// <summary>
/// Queues a transactional/system email (invite, future password recovery, ...) for async delivery.
/// This is a separate, simpler pipeline from <see cref="Notifications.INotificationEnqueuer"/>, which
/// is rule/event-driven and routes to arbitrary admin-configured channels — system notifications
/// always go to one specific recipient via the system-default SMTP provider.
/// </summary>
public interface ISystemNotificationEnqueuer
{
    Task EnqueueAsync(SystemNotificationType type, string recipientEmail,
        IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken = default);
}
