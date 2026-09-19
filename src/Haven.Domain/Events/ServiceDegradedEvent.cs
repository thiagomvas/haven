using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Domain.Events;

public sealed record ServiceDegradedEvent(
    Guid ServiceId,
    string Name,
    string? HealthCheckName = null,
    HealthCheckFailureReason Reason = HealthCheckFailureReason.None,
    string? Message = null) : DomainEvent, IScopedDomainEvent
{
    public NotificationScope PrimaryScope => NotificationScope.Service;
    public Guid PrimaryScopeId => ServiceId;

    public override string ToMessage()
    {
        var message = $"Service \"{Name}\" ({ServiceId}) is degraded";

        if (!string.IsNullOrWhiteSpace(HealthCheckName))
            message += $": health check \"{HealthCheckName}\" failed";

        if (Reason != HealthCheckFailureReason.None)
            message += $" ({Reason})";

        if (!string.IsNullOrWhiteSpace(Message))
            message += $" - {Message}";

        return message;
    }
}
