namespace Haven.Domain.Events;

public abstract record SidecarLifetimeDomainEvent(Guid SidecarId, string Name) : DomainEvent;