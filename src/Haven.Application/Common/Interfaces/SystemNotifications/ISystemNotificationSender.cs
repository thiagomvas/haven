using Haven.Application.Common;
using Haven.Domain;

namespace Haven.Application.Common.Interfaces.SystemNotifications;

/// <summary>Synchronously renders and sends a system notification via the system-default SMTP provider.</summary>
public interface ISystemNotificationSender
{
    Task<Result> SendAsync(SystemNotificationType type, string recipientEmail,
        IReadOnlyDictionary<string, string> templateData, CancellationToken cancellationToken = default);
}
