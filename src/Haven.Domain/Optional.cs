using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haven.Domain;

[JsonConverter(typeof(OptionalJsonConverterFactory))]
public readonly struct Optional<T>
{
    private readonly T _value;

    public bool HasValue { get; }
    public T Value => HasValue ? _value : throw new InvalidOperationException("Optional has no value");

    private Optional(T? value)
    {
        if (value is not null)
        {
            _value = value;
            HasValue = true;
        }
        else
        {
            HasValue = false;
            _value = default;
        }

    }

    public static Optional<T> None => default;
    public static implicit operator Optional<T>(T? value) => new(value);
}

public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        return (JsonConverter)Activator.CreateInstance(
            typeof(OptionalJsonConverter<>).MakeGenericType(valueType))!;
    }
}

public sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
{
    public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null values for Optional properties
        if (reader.TokenType == JsonTokenType.Null)
        {
            return default; // Optional<T>.None
        }

        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return value!; // implicit operator → Some(value)
    }

    public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            JsonSerializer.Serialize(writer, value.Value, options);
        else
            writer.WriteNullValue();
    }
}