using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceCreatedEvent(Project Project, Environment Environment, Service Service) : DomainEvent, IEntityCreatedEvent
{
    Entity IEntityCreatedEvent.CreatedEntity => Service;
}
