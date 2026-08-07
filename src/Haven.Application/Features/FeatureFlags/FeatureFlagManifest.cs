using Haven.Domain;
using Haven.Domain.Enums;

namespace Haven.Application.Features.FeatureFlags;

public class FeatureFlagManifest
{
    public string Name { get; set; }
    public FeatureFlagType Type { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string? Key { get; set; }
    public string Value { get; set; }
    public FeatureFlagValueType ValueType { get; set; }
}