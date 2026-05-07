namespace Haven.Domain.Events;

public record EnvironmentVariablesUpdatedEvent(Guid ParentId, EnvironmentVariableParentType Type) : DomainEvent
{
    public override string ToMessage()
    {
        return $"Environment variables were updated for {Type} with ID '{Id}'";
    }
}