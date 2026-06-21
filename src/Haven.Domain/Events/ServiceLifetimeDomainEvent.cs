namespace Haven.Domain.Events;

public abstract record ServiceLifetimeDomainEvent(Guid ServiceId, string Name) : DomainEvent;