namespace Haven.Domain.Entities;

public class NotificationAttempt : Entity
{
    public Guid RuleId { get; set; }
    public Guid ChannelConfigId { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Webhook;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Response { get; set; }
    public NotificationDeliveryStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? AttemptedAt { get; set; }
    
    public NotificationRule? Rule { get; set; }
}