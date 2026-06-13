namespace Haven.Domain.Entities;

public class NotificationAttempt : Entity
{
    public Guid RuleId { get; private set; }
    public Guid ChannelConfigId { get; private set; }
    public NotificationChannel Channel { get; private set; } = NotificationChannel.Webhook;
    public string EventType { get; private set; } = string.Empty;
    public string EventPayload { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public string? Response { get; private set; }
    public NotificationDeliveryStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? AttemptedAt { get; private set; }

    public NotificationRule? Rule { get; set; }

    public static NotificationAttempt CreateEnqueued(
        Guid ruleId,
        Guid channelConfigId,
        NotificationChannel channel,
        string eventType,
        string eventPayload)
        => new()
        {
            RuleId = ruleId,
            ChannelConfigId = channelConfigId,
            Channel = channel,
            EventType = eventType,
            EventPayload = eventPayload,
            Status = NotificationDeliveryStatus.Pending
        };

    public void MarkDelivered(string sentPayload, string? response)
    {
        Status = NotificationDeliveryStatus.Delivered;
        Payload = sentPayload;
        Response = response;
        AttemptedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string sentPayload, string? response, string errorMessage)
    {
        Status = NotificationDeliveryStatus.Failed;
        Payload = sentPayload;
        Response = response;
        ErrorMessage = errorMessage;
        AttemptedAt = DateTime.UtcNow;
    }
}