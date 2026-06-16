using Haven.Domain;

namespace Haven.Domain.Events;

public interface IScopedDomainEvent
{
    NotificationScope PrimaryScope { get; }
    Guid PrimaryScopeId { get; }
}