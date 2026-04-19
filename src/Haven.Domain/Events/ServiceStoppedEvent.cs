using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceStoppedEvent(Project Project, Environment Environment, Service Service) : DomainEvent;
