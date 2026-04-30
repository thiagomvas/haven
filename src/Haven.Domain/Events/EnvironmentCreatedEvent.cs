using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Mediator;
using Environment = Haven.Domain.Entities.Environment;

namespace Haven.Domain.Events;

public sealed record EnvironmentCreatedEvent(Guid ProjectId, Guid EnvironmentId, string ProjectName, string EnvironmentName) : DomainEvent
{
    public override string ToMessage() => $"Environment \"{EnvironmentName}\" ({EnvironmentId})  was created in \"{ProjectName}\" ({ProjectId})";
}
