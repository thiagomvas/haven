using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Infrastructure.Persistence.Manifests;

public static class YamlSerializerPresets
{
    // Unmatched properties are ignored so legacy manifests that still carry parent ids
    // (e.g. environment.projectId) keep loading; parent ids come from the folder hierarchy.
    public static IDeserializer CreateDeserializer() => new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public static ISerializer CreateSerializer() => new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
}