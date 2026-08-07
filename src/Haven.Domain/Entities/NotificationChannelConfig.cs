using Haven.Domain.Enums;

namespace Haven.Domain.Entities;

public class NotificationChannelConfig : Entity
{
    public string Name { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public string Config { get; set; } = string.Empty;
    public bool Enabled { get; set; }

    /// <summary>
    /// Marks this config as the one used to send transactional/system emails (e.g. invites,
    /// password recovery). Only one config per channel should have this set at a time; that
    /// invariant is enforced by the handler that toggles it (it must clear any sibling first),
    /// not by this entity, since the entity has no visibility into other rows.
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

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

    public void SetEnabled(bool enabled) => Enabled = enabled;

    public void SetAsSystemDefault() => IsSystemDefault = true;
    public void ClearSystemDefault() => IsSystemDefault = false;
}