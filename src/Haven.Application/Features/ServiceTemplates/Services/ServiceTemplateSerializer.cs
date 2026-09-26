using Haven.Application.Features.ServiceTemplates.Contracts;
using Version = Haven.Domain.ValueObjects.Version;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Haven.Application.Features.ServiceTemplates.Services;

public class ServiceTemplateSerializer
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithCaseInsensitivePropertyMatching()
        .WithTypeConverter(new VersionYamlTypeConverter())
        .Build();

    private static readonly ISerializer _serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .WithTypeConverter(new VersionYamlTypeConverter())
        .WithQuotingNecessaryStrings()
        .Build();

    private sealed class VersionYamlTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type) => type == typeof(Version);

        public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
        {
            var scalar = parser.Consume<Scalar>();
            return Version.Parse(scalar.Value);
        }

        public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
        {
            emitter.Emit(new Scalar(((Version)value!).ToString()));
        }
    }
    
    public Task<ServiceTemplate> DeserializeAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var yaml = reader.ReadToEnd();
        var template = _deserializer.Deserialize<ServiceTemplate>(yaml);
        return Task.FromResult(template);
    }

    public Task<string> SerializeAsync(ServiceTemplate template)
    {
        var yaml = _serializer.Serialize(template);
        return Task.FromResult(yaml);
    }
}