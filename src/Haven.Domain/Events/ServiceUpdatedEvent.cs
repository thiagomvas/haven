using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record ServiceUpdatedEvent(Guid Id, string Name, string EnvironmentName, string ProjectName, string? OldName = null) : DomainEvent
{
    public override string ToMessage()
    {
        if (!string.IsNullOrWhiteSpace(OldName))
            return $"Service \"{Name}\" ({Id}, formerly {OldName}) was updated in \"{EnvironmentName}\" ({ProjectName})";
        return $"Service \"{Name}\" ({Id}) was updated in \"{EnvironmentName}\" ({ProjectName})";    }
}
