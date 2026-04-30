using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceCreatedEvent(Guid Id, string Name, string EnvironmentName, string ProjectName) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({Id}) service was created in \"{EnvironmentName}\" ({ProjectName})";
}
