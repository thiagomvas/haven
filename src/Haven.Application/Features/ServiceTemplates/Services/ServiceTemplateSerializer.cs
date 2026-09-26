using Haven.Application.Features.ServiceTemplates.Contracts;

using YamlDotNet.Serialization;

namespace Haven.Application.Features.ServiceTemplates.Services;

public class ServiceTemplateSerializer
{
    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .IgnoreUnmatchedProperties()
        .WithCaseInsensitivePropertyMatching()
        .Build();
    
    private static readonly ISerializer _serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitDefaults | DefaultValuesHandling.OmitEmptyCollections)
        .Build();
    
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