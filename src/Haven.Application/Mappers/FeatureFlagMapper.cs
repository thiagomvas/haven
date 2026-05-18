using Haven.Application.Features.FeatureFlags;
using Haven.Application.Features.FeatureFlags.Commands.UpdateFeatureFlagCommand;
using Haven.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace Haven.Application.Mappers;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.None)]
public static partial class FeatureFlagMapper
{
    internal static partial FeatureFlagDto ToDtoPartial(this FeatureFlag flag);
    private static partial FeatureFlagManifest ToManifestPartial(this FeatureFlag flag);

    public static FeatureFlagDto ToDto(this FeatureFlag flag)
    {
        return flag.ToDtoPartial();
    }

    public static FeatureFlagManifest ToManifest(this FeatureFlag flag)
    {
        return flag.ToManifestPartial();
    }
    
    public static FeatureFlag Ingest(this FeatureFlag featureFlag, UpdateFeatureFlagCommand command)
    {
        featureFlag.Name = command.Name ?? featureFlag.Name;
        featureFlag.Description = command.Description ?? featureFlag.Description;
        featureFlag.Type = command.Type ?? featureFlag.Type;
        featureFlag.Value = command.Value ?? featureFlag.Value;
        featureFlag.ValueType = command.ValueType ?? featureFlag.ValueType;
        return featureFlag;
    }
}