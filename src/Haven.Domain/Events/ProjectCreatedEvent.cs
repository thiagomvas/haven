using Haven.Domain.Aggregates;
using Mediator;

namespace Haven.Domain.Events;

public sealed record ProjectCreatedEvent(Project Project) : DomainEvent;