using Haven.Domain.Entities;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.CustomActions;

public sealed class CustomActionDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ActionConfig Config { get; set; } = default!;
    public string[] RequiredPermissions { get; set; } = [];
    public ActionRisk Risk { get; set; }
    public TimeSpan Timeout { get; set; }
    public CustomActionInput[] Inputs { get; set; } = [];
    public string WebhookUrl { get; set; } = string.Empty;
}
