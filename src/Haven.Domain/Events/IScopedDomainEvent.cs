using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public interface IScopedDomainEvent
{
    NotificationScope PrimaryScope { get; }
    Guid PrimaryScopeId { get; }
}