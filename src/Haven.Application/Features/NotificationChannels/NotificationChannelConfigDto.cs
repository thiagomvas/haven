using Haven.Domain;

namespace Haven.Application.Features.NotificationChannels;

public record NotificationChannelConfigDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    string Config,
    bool Enabled,
    int RulesCount);