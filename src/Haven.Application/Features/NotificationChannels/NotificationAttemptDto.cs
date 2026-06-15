using Haven.Domain;

namespace Haven.Application.Features.NotificationChannels;

public record NotificationAttemptDto(
    Guid Id,
    string EventType,
    NotificationDeliveryStatus Status,
    string? ErrorMessage,
    DateTime? AttemptedAt);
