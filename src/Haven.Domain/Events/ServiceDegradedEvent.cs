using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceDegradedEvent(Guid Id, string Name, string EnvironmentName, string ProjectName) : DomainEvent
{
    public override string ToMessage() => $"Service \"{Name}\" ({Id}) is degraded in \"{EnvironmentName}\" ({ProjectName})";
}
