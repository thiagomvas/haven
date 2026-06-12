using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public class NotificationChannelConfig : Entity
{
    public string Name  { get; set; }
    public NotificationChannel Channel { get; set; }
    public EncryptedValue Config { get; set; }
    public bool Enabled { get; set; }
    
    public ICollection<NotificationRule> NotificationRules { get; set; } = [];
}