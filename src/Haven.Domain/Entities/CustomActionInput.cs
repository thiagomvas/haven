namespace Haven.Domain.Entities;

/// <summary>
/// Declares a value a caller supplies when executing a <see cref="CustomAction"/>. The value is
/// referenced from the action's config as <c>${{ inputs.&lt;Name&gt; }}</c>.
/// </summary>
/// <param name="Name">Key used in templates; letters, digits, '_' and '-'.</param>
/// <param name="Label">Human readable label shown when prompting for the value.</param>
/// <param name="Description">Optional help text.</param>
/// <param name="Required">Whether a value must be available (supplied or defaulted) at execution time.</param>
/// <param name="DefaultValue">Used when the caller doesn't supply a value.</param>
public sealed record CustomActionInput(
    string Name,
    string Label,
    string? Description = null,
    bool Required = false,
    string? DefaultValue = null);