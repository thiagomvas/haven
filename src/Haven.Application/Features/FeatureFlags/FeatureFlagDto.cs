using Haven.Domain;

namespace Haven.Application.Features.FeatureFlags;

public class FeatureFlagDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string Name { get; set; }
    public FeatureFlagType Type { get; set; }
    public string? Description { get; set; } = string.Empty;
    public string Value { get; set; }
    public FeatureFlagValueType ValueType { get; set; }
}