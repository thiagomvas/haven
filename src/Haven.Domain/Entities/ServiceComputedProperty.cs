using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Haven.Domain.Aggregates;
using Haven.Domain.Exceptions;

namespace Haven.Domain.Entities;

/// <summary>
/// A read-only derived value declared by a service template (e.g. a connection string),
/// captured on the service at instantiation time. <see cref="Template"/> may still contain
/// unresolved placeholders (e.g. <c>${{ env.PASSWORD }}</c>, <c>${{ runtime.host }}</c>) that
/// are resolved at read time, so no secret value is ever persisted here.
/// </summary>
public sealed partial class ServiceComputedProperty : Entity
{
    public Guid ServiceId { get; set; }
    public string Key { get; set; } = default!;
    public string Label { get; set; } = default!;
    public string Template { get; set; } = default!;
    public bool IsSecret { get; set; }
    public DateTime CreatedAt { get; set; }

    [JsonIgnore] public Service? Service { get; set; }

    private ServiceComputedProperty() { }

    public static ServiceComputedProperty Create(
        Guid serviceId,
        string key,
        string label,
        string template,
        bool isSecret)
    {
        key = key?.Trim() ?? string.Empty;
        label = label?.Trim() ?? string.Empty;
        template = template?.Trim() ?? string.Empty;

        Validate(key, label, template);

        return new ServiceComputedProperty
        {
            ServiceId = serviceId,
            Key = key,
            Label = label,
            Template = template,
            IsSecret = isSecret,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ServiceComputedProperty Reconstitute(
        Guid id,
        Guid serviceId,
        string key,
        string label,
        string template,
        bool isSecret,
        DateTime createdAt)
    {
        var property = new ServiceComputedProperty
        {
            ServiceId = serviceId,
            Key = key,
            Label = label,
            Template = template,
            IsSecret = isSecret,
            CreatedAt = createdAt
        };
        property.Id = id;
        return property;
    }

    private static void Validate(string key, string label, string template)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ValidationException("Computed property key is required.");

        if (!KeyPattern().IsMatch(key))
            throw new ValidationException("Computed property key must be lowercase alphanumeric with underscores.");

        if (string.IsNullOrWhiteSpace(label))
            throw new ValidationException("Computed property label is required.");

        if (string.IsNullOrWhiteSpace(template))
            throw new ValidationException("Computed property template is required.");
    }

    [GeneratedRegex(@"^[a-z0-9_]+$", RegexOptions.Compiled)]
    private static partial Regex KeyPattern();
}
