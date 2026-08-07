using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels;

public record NotificationAttemptDto(
    Guid Id,
    Guid ChannelConfigId,
    string ChannelConfigName,
    NotificationChannel Channel,
    string EventType,
    NotificationDeliveryStatus Status,
    string? ErrorMessage,
    DateTime? AttemptedAt,
    string EventPayload,
    string? Payload,
    string? Response);