using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceDegradedEvent(Project Project, Environment Environment, Service Service) : DomainEvent
{
    public override string ToMessage() => $"\"{Service.Name}\" service is degraded in \"{Environment.Name}\" ({Project.Name})";
}
