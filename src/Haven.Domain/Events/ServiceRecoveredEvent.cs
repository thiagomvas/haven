using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record ServiceRecoveredEvent(Guid ServiceId, string Name, string? HealthCheckName = null) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Service;
    public Guid PrimaryScopeId => ServiceId;

    public override string ToMessage()
    {
        var message = $"Service \"{Name}\" ({ServiceId}) has recovered and is healthy again";

        return string.IsNullOrWhiteSpace(HealthCheckName)
            ? message
            : $"{message} (health check \"{HealthCheckName}\" passed)";
    }
}
