namespace Haven.Domain.Entities;

public class NotificationChannelConfig : Entity
{
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Config { get; set; } = string.Empty;
    public bool Enabled { get; set; }

    public ICollection<NotificationRule> NotificationRules { get; set; } = [];

    public static NotificationChannelConfig Create(string name, NotificationChannel channel, string configJson, bool enabled) =>
        new()
        {
            Name = name,
            Channel = channel,
            Config = configJson,
            Enabled = enabled,
        };

    public void Update(string name, string configJson, bool enabled)
    {
        Name = name;
        Config = configJson;
        Enabled = enabled;
    }
}