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
    
    public void RegenerateToken()
    {
        Token = $"hca_{Guid.NewGuid().ToString("N")}";
    }
}