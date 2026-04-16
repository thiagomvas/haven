using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentUpdatedEvent(Project Project, Environment Environment, string OldName) : DomainEvent;
