using Haven.Domain.Aggregates;
using Haven.Domain.ValueObjects;

namespace Haven.Domain.Entities;

public sealed class CustomAction : Entity
{
    public Guid ServiceId { get; set; }
    public string ActionName { get; set; }
    public string Alias { get; set; }
    public string ActionDescription { get; set; }
    public string Icon { get; set; }
    public ActionConfig Config { get; set; }
    public string[] RequiredPermissions { get; set; }
    public ActionRisk Risk { get; set; }
    public TimeSpan Timeout { get; set; }
    public string Token { get; set; }
    public Service? Service { get; set; }

    private CustomAction()
    {
        // Required by EF
    }

    public static CustomAction Create(Guid serviceId, string actionName, string alias, string actionDescription,
        string icon, ActionConfig config, string[] requiredPermissions, ActionRisk risk, TimeSpan timeout)
    {
        var customAction = new CustomAction
        {
            ServiceId = serviceId,
            ActionName = actionName,
            Alias = alias,
            ActionDescription = actionDescription,
            Icon = icon,
            Config = config,
            RequiredPermissions = requiredPermissions,
            Risk = risk,
            Timeout = timeout
        };

        customAction.RegenerateToken();

        return customAction;
    }
    
    public void Update(Optional<string> actionName, Optional<string> alias, Optional<string> actionDescription,
        Optional<string> icon, Optional<ActionConfig> config, Optional<string[]> requiredPermissions,
        Optional<ActionRisk> risk, Optional<TimeSpan> timeout)
    {
        if (actionName.HasValue) ActionName = actionName.Value;
        if (alias.HasValue) Alias = alias.Value;
        if (actionDescription.HasValue) ActionDescription = actionDescription.Value;
        if (icon.HasValue) Icon = icon.Value;
        if (config.HasValue) Config = config.Value;
        if (requiredPermissions.HasValue) RequiredPermissions = requiredPermissions.Value;
        if (risk.HasValue) Risk = risk.Value;
        if (timeout.HasValue) Timeout = timeout.Value;
    }

    /// <summary>
    /// Returns a detached copy of this action carrying <paramref name="config"/>, leaving the tracked instance untouched.
    /// </summary>
    public CustomAction WithConfig(ActionConfig config)
    {
        var copy = (CustomAction)MemberwiseClone();
        copy.Config = config;
        return copy;
    }

    public void RegenerateToken()
    {
        Token = $"hca_{Guid.NewGuid().ToString("N")}";
    }
}