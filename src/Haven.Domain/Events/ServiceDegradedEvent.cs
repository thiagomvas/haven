namespace Haven.Domain.Events;

public sealed record ServiceDegradedEvent(Guid ServiceId, string Name) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({ServiceId}) is degraded";
}