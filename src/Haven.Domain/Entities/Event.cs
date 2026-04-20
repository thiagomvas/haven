namespace Haven.Domain.Entities;

public sealed class Event : Entity
{
    public string EventType { get; private set; } = default!;
    public string? Payload { get; private set; }
    public DateTime TriggeredAt { get; private set; }

    private Event() { }

    public static Event Create(string eventType, string? payload = null) =>
        new()
        {
            EventType = eventType,
            Payload = payload,
            TriggeredAt = DateTime.UtcNow,
        };

    public static Event Reconstitute(Guid id, string eventType, string? payload, DateTime triggeredAt) =>
        new()
        {
            Id = id,
            EventType = eventType,
            Payload = payload,
            TriggeredAt = triggeredAt,
        };
}
