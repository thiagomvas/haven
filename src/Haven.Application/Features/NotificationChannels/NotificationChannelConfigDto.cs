using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.NotificationChannels;

public record NotificationChannelConfigDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    string Config,
    bool Enabled,
    bool IsSystemDefault,
    int RulesCount);