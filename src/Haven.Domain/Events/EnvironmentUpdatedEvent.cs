using Haven.Domain.Aggregates;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentUpdatedEvent(Guid Id, string NewName, string ProjectName, string? OldName = null) : DomainEvent
{
    public override string ToMessage()
    {
        if (string.IsNullOrWhiteSpace(OldName))
            return $"Environment {NewName} ({Id}) was updated in \"{ProjectName}\"";
        return $"Environment \"{NewName}\" ({Id}, formerly {OldName}) was updated in \"{ProjectName}\"";
    }
}
