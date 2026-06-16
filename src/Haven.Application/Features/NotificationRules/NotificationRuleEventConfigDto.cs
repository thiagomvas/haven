namespace Haven.Application.Features.NotificationRules;

public record NotificationRuleEventConfigDto(string EventType, IReadOnlyList<Guid> ChannelIds);