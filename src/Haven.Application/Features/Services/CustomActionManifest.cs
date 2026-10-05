using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

namespace Haven.Application.Features.Services;

/// <summary>
/// YAML-serializable representation of a <see cref="CustomAction"/> for manifest files. The polymorphic
/// <see cref="ActionConfig"/> is flattened into <see cref="Config"/>. The action token is intentionally
/// not persisted; it is regenerated on import.
/// </summary>
public sealed class CustomActionManifest
{
    public Guid Id { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public ActionConfigManifest Config { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = [];
    public ActionRisk Risk { get; set; }
    public TimeSpan Timeout { get; set; }
    public List<CustomActionInputManifest> Inputs { get; set; } = [];
}

/// <summary>Flattened <see cref="ActionConfig"/>. <see cref="Type"/> is <c>exec</c> or <c>http</c>.</summary>
public sealed class ActionConfigManifest
{
    public string Type { get; set; } = string.Empty;

    // exec
    public List<string>? Command { get; set; }
    public string? WorkingDir { get; set; }
    public string? User { get; set; }
    public ShellType? Shell { get; set; }

    // http
    public string? Method { get; set; }
    public string? Url { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Body { get; set; }
    public List<int>? SuccessStatusCodes { get; set; }
}

public sealed class CustomActionInputManifest
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
}