using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentDeletedEvent(Guid Id, string Name, string ProjectName) : DomainEvent
{
    public override string ToMessage() => $"Environment \"{Name}\" ({Id}) was deleted from \"{ProjectName}\"";
}
