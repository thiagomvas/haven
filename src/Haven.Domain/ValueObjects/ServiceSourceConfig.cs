using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type", UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
[JsonDerivedType(typeof(DockerBasedSourceConfig), "docker-based")]
[JsonDerivedType(typeof(DockerConfig), "docker")]
[JsonDerivedType(typeof(DockerfileConfig), "dockerfile")]
public abstract class ServiceSourceConfig { }
