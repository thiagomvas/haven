using Haven.Application.Features.Services;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.ServiceTemplates.Contracts;

/// <summary>
/// A custom action created alongside a service instantiated from the template. String values in
/// <see cref="Config"/> and the input defaults may reference the template's inputs via
/// <c>${{ inputs.key }}</c>; any other placeholder (e.g. an action input) is left as-is for
/// execution time. Referencing a <c>secret</c> template input is not allowed.
/// </summary>
public class ServiceTemplateAction
{
    public string Name { get; set; } = null!;
    public string Alias { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "play";
    public ActionConfigManifest Config { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public ActionRisk Risk { get; set; } = ActionRisk.Safe;
    public int TimeoutSeconds { get; set; } = 30;
    public List<CustomActionInputManifest> Inputs { get; set; } = new();
}