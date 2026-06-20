using System.Text.Json.Serialization;

namespace Haven.Domain.Entities;

public class FeatureFlag : Entity
{
    public Guid ServiceId { get; set; }
    public string Name { get; set; }
    public FeatureFlagType Type { get; set; }
    public string? Key { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string Value { get; set; }
    public FeatureFlagValueType ValueType { get; set; }

    [JsonIgnore] public Service? Service { get; set; } = null;

    private FeatureFlag()
    {
    }

    public static FeatureFlag Create(Guid serviceId, string name, FeatureFlagType type, string? key, string? description,
        string value, FeatureFlagValueType valueType) =>
        new()
        {
            ServiceId = serviceId,
            Name = name,
            Type = type,
            Key = key,
            Description = description,
            Value = value,
            ValueType = valueType
        };

    public static FeatureFlag Reconstitute(Guid id, Guid serviceId, string name, FeatureFlagType type, string? key, string? description,
        string value, FeatureFlagValueType valueType) =>
        new()
        {
            Id = id,
            ServiceId = serviceId,
            Name = name,
            Type = type,
            Key = key,
            Description = description,
            Value = value,
            ValueType = valueType
        };
}