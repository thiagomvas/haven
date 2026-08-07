using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;

namespace Haven.Application.Common.Interfaces.Notifications;

public interface INotificationProvider
{
    NotificationChannel Channel { get; }
    Task<NotificationProviderResult> SendAsync(NotificationAttempt attempt, NotificationChannelConfig config, CancellationToken ct = default);
}