using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceCreatedEvent(Project Project, Environment Environment, Service Service) : DomainEvent
{
    public override string ToMessage() => $"\"{Service.Name}\" service was created in \"{Environment.Name}\" ({Project.Name})";
}
