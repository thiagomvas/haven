using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DockerConfig), "docker")]
public abstract class ServiceSourceConfig { }
