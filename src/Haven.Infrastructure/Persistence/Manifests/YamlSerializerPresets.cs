using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Haven.Infrastructure.Persistence.Manifests;

public static class YamlSerializerPresets
{
    public static IDeserializer CreateDeserializer() => new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public static ISerializer CreateSerializer() => new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
}