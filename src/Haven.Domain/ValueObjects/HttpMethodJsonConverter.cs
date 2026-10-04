using System.Text.Json;
using System.Text.Json.Serialization;

namespace Haven.Domain.ValueObjects;

/// <summary>
/// Serializes <see cref="HttpMethod"/> as a plain string (e.g. "GET").
/// Also accepts the legacy <c>{"method":"GET"}</c> object shape when reading.
/// </summary>
public sealed class HttpMethodJsonConverter : JsonConverter<HttpMethod>
{
    public override HttpMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new HttpMethod(reader.GetString()!);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? method = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName) continue;
                var name = reader.GetString();
                reader.Read();
                if (string.Equals(name, "method", StringComparison.OrdinalIgnoreCase))
                    method = reader.GetString();
                else
                    reader.Skip();
            }

            if (!string.IsNullOrWhiteSpace(method))
                return new HttpMethod(method);
        }

        throw new JsonException("Invalid HTTP method.");
    }

    public override void Write(Utf8JsonWriter writer, HttpMethod value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Method);
}
