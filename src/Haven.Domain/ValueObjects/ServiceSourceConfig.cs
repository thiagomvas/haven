using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DockerConfig), "docker")]
[JsonDerivedType(typeof(DockerfileConfig), "dockerfile")]
public abstract class ServiceSourceConfig { }
