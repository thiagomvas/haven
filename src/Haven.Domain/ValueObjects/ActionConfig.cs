using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExecActionConfig), "exec")]
[JsonDerivedType(typeof(HttpActionConfig), "http")]
public abstract record ActionConfig;