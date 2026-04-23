using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceRestartedEvent(Project Project, Environment Environment, Service Service) : DomainEvent
{
    public override string ToMessage() => $"\"{Service.Name}\" service was restarted in \"{Environment.Name}\" ({Project.Name})";
}
