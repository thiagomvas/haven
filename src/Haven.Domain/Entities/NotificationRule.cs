namespace Haven.Domain.Entities;

public class NotificationRule : Entity
{
    public Guid ChannelConfigId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public NotificationScope Scope { get; set; } = NotificationScope.Global;
    public Guid? ScopeId { get; set; }
    public bool Enabled { get; set; }
    
    public NotificationChannelConfig? ChannelConfig { get; set; }
    public ICollection<NotificationAttempt> NotificationAttempts { get; set; } = [];
}