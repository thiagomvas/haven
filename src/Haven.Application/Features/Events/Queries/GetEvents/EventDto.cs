namespace Haven.Application.Features.Events.Queries.GetEvents;

public sealed record EventDto(Guid Id, string EventType, string Message, string? Payload, DateTime TriggeredAt);
