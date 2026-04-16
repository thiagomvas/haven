using Haven.Domain.Aggregates;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentCreatedEvent(Project Project, Environment Environment) : DomainEvent;
