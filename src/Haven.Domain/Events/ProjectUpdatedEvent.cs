using Haven.Domain.Aggregates;

namespace Haven.Domain.Events;

public sealed record ProjectUpdatedEvent(Project Project) : DomainEvent;