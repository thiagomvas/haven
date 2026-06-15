using Haven.Domain;

namespace Haven.Domain.Events;

public record EnvironmentVariablesUpdatedEvent(Guid ParentId, EnvironmentVariableParentType Type) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => Type switch
    {
        EnvironmentVariableParentType.Service => NotificationScope.Service,
        EnvironmentVariableParentType.Environment => NotificationScope.Environment,
        EnvironmentVariableParentType.Project => NotificationScope.Project,
        _ => NotificationScope.Global,
    };
    public Guid PrimaryScopeId => ParentId;
    public override string ToMessage() => $"Environment variables were updated for {Type} with ID '{ParentId}'";
}
